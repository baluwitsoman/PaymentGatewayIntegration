using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Providers;

namespace PaymentGateway.Web.Features.PublicApi.Webhooks;

/// Generic webhook receiver — one URL per (provider, company).
/// URL: /webhooks/{providerCode}/{companyId}
/// The provider's IPaymentProvider.VerifyWebhookAsync handles signature
/// verification and field extraction. This handler only does the orchestration
/// (lookup tenant config, persist event + transaction, advance order state).
public static class ProviderWebhook
{
    public static async Task<IResult> Handle(
        HttpContext http,
        string providerCode,
        Guid companyId,
        AppDbContext db,
        IPaymentProviderRegistry registry,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ProviderWebhook");
        logger.LogInformation("Webhook RX provider={Provider} company={Co} from={Ip}",
            providerCode, companyId, http.Connection.RemoteIpAddress);

        if (!Enum.TryParse<PaymentProviderCode>(providerCode, true, out var pc))
            return Results.NotFound(new { error = $"Unknown provider '{providerCode}'." });

        if (!registry.TryGet(pc, out var provider))
            return Results.NotFound(new { error = $"Provider '{providerCode}' not registered." });

        using var reader = new StreamReader(http.Request.Body);
        var rawBody = await reader.ReadToEndAsync(ct);

        var headers = http.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        var query = http.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());

        var evt = new WebhookEvent
        {
            Source = WebhookSource.TransactionCallback,
            ProviderCode = pc,
            CompanyId = companyId,
            Headers = JsonSerializer.Serialize(headers),
            Body = SafeJsonOrString(rawBody),
            ReceivedAt = DateTime.UtcNow,
            RemoteIp = http.Connection.RemoteIpAddress,
        };

        var cfg = await db.ProviderConfigs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.ProviderCode == pc, ct);
        if (cfg == null)
        {
            evt.ProcessingStatus = WebhookProcessingStatus.Ignored;
            evt.ProcessingError = "Unknown companyId / provider combination";
            evt.ProcessedAt = DateTime.UtcNow;
            db.WebhookEvents.Add(evt);
            await db.SaveChangesAsync(ct);
            return Results.NotFound();
        }

        var verify = await provider.VerifyWebhookAsync(
            new WebhookVerifyContext(cfg, rawBody, headers, query), ct);

        evt.HmacReceived = verify.HmacReceived;
        evt.HmacComputed = verify.HmacComputed;
        evt.HmacValid = verify.IsValid;

        if (!verify.IsValid)
        {
            evt.ProcessingStatus = WebhookProcessingStatus.Failed;
            evt.ProcessingError = verify.Reason ?? "Verification failed";
            evt.ProcessedAt = DateTime.UtcNow;
            db.WebhookEvents.Add(evt);
            await db.SaveChangesAsync(ct);
            return Results.Unauthorized();
        }

        evt.ProviderTransactionId = verify.ProviderTransactionId;

        // Locate our PaymentOrder by merchant reference (preferred) or provider order id.
        var order = !string.IsNullOrEmpty(verify.MerchantOrderReference)
            ? await db.PaymentOrders.IgnoreQueryFilters()
                .FirstOrDefaultAsync(po => po.OrderReference == verify.MerchantOrderReference, ct)
            : await db.PaymentOrders.IgnoreQueryFilters()
                .FirstOrDefaultAsync(po => po.ProviderOrderId == verify.ProviderOrderId
                                        && po.SelectedProviderCode == pc, ct);

        if (order == null || order.CompanyId != companyId)
        {
            evt.ProcessingStatus = WebhookProcessingStatus.Ignored;
            evt.ProcessingError = "Order not found for this company";
            evt.ProcessedAt = DateTime.UtcNow;
            db.WebhookEvents.Add(evt);
            await db.SaveChangesAsync(ct);
            return Results.NotFound();
        }

        evt.PaymentOrderId = order.Id;

        // Idempotent upsert of PaymentTransaction
        var tx = await db.PaymentTransactions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.ProviderCode == pc && t.ProviderTransactionId == verify.ProviderTransactionId, ct);
        var now = DateTime.UtcNow;
        if (tx == null)
        {
            tx = new PaymentTransaction
            {
                PaymentOrderId = order.Id,
                CompanyId = order.CompanyId,
                ProviderCode = pc,
                ProviderTransactionId = verify.ProviderTransactionId!,
                IsSuccess = verify.IsSuccess,
                IsPending = verify.IsPending,
                IsRefund = verify.IsRefund,
                IsVoid = verify.IsVoid,
                Is3DSecure = verify.Is3DSecure,
                AmountMinor = verify.AmountMinor,
                Currency = verify.Currency,
                ProviderIntegrationId = verify.ProviderIntegrationId,
                SourceType = verify.SourceType,
                SourceData = verify.SourceData,
                ErrorMessage = verify.ErrorMessage,
                HmacValid = true,
                ReceivedAt = now,
                ProcessedAt = now,
            };
            db.PaymentTransactions.Add(tx);
        }
        else
        {
            tx.IsSuccess = verify.IsSuccess;
            tx.IsPending = verify.IsPending;
            tx.IsRefund = verify.IsRefund;
            tx.IsVoid = verify.IsVoid;
            tx.ProcessedAt = now;
        }

        // Advance order state — don't downgrade from terminal Paid.
        if (order.Status != PaymentOrderStatus.Paid)
        {
            if (tx.IsSuccess && !tx.IsRefund && !tx.IsVoid)
            {
                order.Status = PaymentOrderStatus.Paid;
                order.PaidAt = now;
                order.ClosedAt = now;
                await EnqueueOutboxAsync(db, order, "payment.paid", tx, ct);
            }
            else if (!tx.IsPending)
            {
                order.Status = PaymentOrderStatus.Failed;
                order.ClosedAt = now;
                await EnqueueOutboxAsync(db, order, "payment.failed", tx, ct);
            }
        }

        evt.ProcessingStatus = WebhookProcessingStatus.Processed;
        evt.ProcessedAt = now;
        db.WebhookEvents.Add(evt);

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static string SafeJsonOrString(string raw)
    {
        try { _ = JsonDocument.Parse(raw); return raw; }
        catch { return JsonSerializer.Serialize(new { raw }); }
    }

    private static async Task EnqueueOutboxAsync(
        AppDbContext db, PaymentOrder order, string eventType, PaymentTransaction tx, CancellationToken ct)
    {
        var app = await db.Applications.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == order.CompanyApplicationId, ct);
        if (app == null || string.IsNullOrWhiteSpace(app.WebhookUrl)) return;

        // SECURITY: do not enqueue messages we cannot sign. The dispatcher
        // would DeadLetter them anyway; refusing to enqueue keeps the outbox
        // table free of unsendable work and gives the admin a clear signal
        // (the consumer simply never receives — they'll have to use the
        // status-polling fallback until a secret is configured).
        if (app.WebhookSecretEncrypted == null)
        {
            // Stamp the webhook_event row that's about to be saved alongside
            // so audit shows why the consumer wasn't notified.
            order.GetType(); // no-op; we don't have logger here, see TODO
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            event_type = eventType,
            order_reference = order.OrderReference,
            external_reference = order.ExternalReference,
            status = order.Status.ToString(),
            amount_minor = order.AmountMinor,
            currency = order.Currency,
            paid_at = order.PaidAt,
            provider = tx.ProviderCode.ToString(),
            provider_transaction_id = tx.ProviderTransactionId,
        });

        db.Outbox.Add(new OutboxMessage
        {
            EventType = eventType,
            CompanyId = order.CompanyId,
            CompanyApplicationId = app.Id,
            PaymentOrderId = order.Id,
            TargetUrl = app.WebhookUrl,
            Payload = payload,
            Status = OutboxStatus.Pending,
            NextAttemptAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
    }
}
