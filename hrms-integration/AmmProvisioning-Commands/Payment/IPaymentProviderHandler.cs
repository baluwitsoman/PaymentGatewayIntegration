using Newtonsoft.Json;

namespace ERPMultiTenent.Application.ERP.AmmProvisioning.Payment;

public interface IPaymentProviderHandler
{
    Task<UnifiedPaymentCommand> ParseAsync(string rawJson);
}

/// Parses the canonical webhook payload emitted by our PaymentGateway orchestrator.
/// The orchestrator already normalises across Paymob / BankMuscat / NBO / future
/// providers, so this is the only handler the HRMS needs.
public class PaymentGatewayHandler : IPaymentProviderHandler
{
    private class WebhookEnvelope
    {
        public string event_type { get; set; } = "";
        public string order_reference { get; set; } = "";
        public string? external_reference { get; set; }
        public string status { get; set; } = "";        // Paid / Failed / Cancelled / Expired
        public long amount_minor { get; set; }
        public string currency { get; set; } = "";
        public DateTime? paid_at { get; set; }
        public string? provider { get; set; }
        public string? provider_transaction_id { get; set; }
    }

    public Task<UnifiedPaymentCommand> ParseAsync(string rawJson)
    {
        var env = JsonConvert.DeserializeObject<WebhookEnvelope>(rawJson)
            ?? throw new InvalidOperationException("Empty webhook body");

        if (!int.TryParse(env.external_reference, out var invoiceId))
            throw new InvalidOperationException(
                $"external_reference '{env.external_reference}' is not a valid invoice id");

        // Canonical status — old code used "Y"/"N"; we use full words but keep
        // "Y"/"N" for backward compat with the repo until it's updated.
        var status = env.status switch
        {
            "Paid" => "Paid",
            "Failed" => "Failed",
            "Cancelled" => "Cancelled",
            "Expired" => "Expired",
            _ => env.status
        };

        return Task.FromResult(new UnifiedPaymentCommand
        {
            InvoiceId = invoiceId,
            TransactionId = env.provider_transaction_id ?? "",
            Amount = env.amount_minor / 100m,
            Status = status,
            Provider = env.provider ?? "Unknown",
            OrderReference = env.order_reference,
            EventType = env.event_type,
            PaidAt = env.paid_at,
        });
    }
}
