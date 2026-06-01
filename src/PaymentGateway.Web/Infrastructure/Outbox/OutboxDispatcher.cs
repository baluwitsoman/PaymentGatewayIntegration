using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Security;

namespace PaymentGateway.Web.Infrastructure.Outbox;

public class OutboxDispatcher : BackgroundService
{
    public const string HttpClientName = "outbox";

    private readonly IServiceProvider _sp;
    private readonly ILogger<OutboxDispatcher> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    public OutboxDispatcher(IServiceProvider sp, ILogger<OutboxDispatcher> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Tiny startup delay so DB has a moment to come up.
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatch loop error");
            }
            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task DispatchBatchAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<ISecretProtector>();
        var clientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var http = clientFactory.CreateClient(HttpClientName);
        http.Timeout = TimeSpan.FromSeconds(15);

        var now = DateTime.UtcNow;
        var batch = await db.Outbox
            .Where(m => m.Status == OutboxStatus.Pending && (m.NextAttemptAt == null || m.NextAttemptAt <= now))
            .OrderBy(m => m.CreatedAt)
            .Take(25)
            .ToListAsync(ct);

        if (batch.Count == 0) return;

        var apps = await db.Applications.IgnoreQueryFilters()
            .Where(a => batch.Select(b => b.CompanyApplicationId).Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);

        foreach (var msg in batch)
        {
            msg.AttemptCount++;
            msg.LastAttemptedAt = DateTime.UtcNow;

            try
            {
                // SECURITY: refuse to dispatch unsigned. If the application has no
                // webhook secret configured, the consumer has no way to verify the
                // message came from us — silently sending would be worse than not
                // sending at all. DeadLetter so it's visible to operators.
                if (!apps.TryGetValue(msg.CompanyApplicationId, out var app)
                    || app.WebhookSecretEncrypted is not { } secretBytes)
                {
                    msg.Status = OutboxStatus.DeadLetter;
                    msg.LastError = "Webhook secret not configured for this application — refused to send unsigned";
                    _logger.LogError(
                        "Refusing to dispatch outbox {Id} for app {App}: no webhook secret",
                        msg.Id, msg.CompanyApplicationId);
                    continue;
                }

                using var req = new HttpRequestMessage(HttpMethod.Post, msg.TargetUrl);
                req.Content = new StringContent(msg.Payload, Encoding.UTF8, "application/json");

                var secretStr = protector.Decrypt(secretBytes);
                var sig = ComputeSignature(secretStr, msg.Payload);
                req.Headers.Add("X-Signature", sig);
                msg.Signature = sig;
                req.Headers.Add("X-Event-Type", msg.EventType);

                var res = await http.SendAsync(req, ct);
                msg.LastResponseCode = (int)res.StatusCode;

                if (res.IsSuccessStatusCode)
                {
                    msg.Status = OutboxStatus.Sent;
                    msg.DeliveredAt = DateTime.UtcNow;
                    msg.LastError = null;
                }
                else
                {
                    msg.LastError = $"HTTP {(int)res.StatusCode}";
                    ScheduleRetry(msg);
                }
            }
            catch (Exception ex)
            {
                msg.LastError = ex.Message[..Math.Min(ex.Message.Length, 500)];
                ScheduleRetry(msg);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static void ScheduleRetry(Domain.Entities.OutboxMessage msg)
    {
        if (msg.AttemptCount >= msg.MaxAttempts)
        {
            msg.Status = OutboxStatus.DeadLetter;
            return;
        }
        var delaySec = Math.Min(900, (int)Math.Pow(2, msg.AttemptCount) * 5);
        msg.NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySec);
    }

    private static string ComputeSignature(string secret, string payload)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
