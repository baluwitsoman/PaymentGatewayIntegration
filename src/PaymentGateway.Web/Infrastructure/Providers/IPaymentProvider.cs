using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Domain.Enums;

namespace PaymentGateway.Web.Infrastructure.Providers;

/// Capabilities the orchestrator needs from any payment provider.
/// Implementations live under Infrastructure/Providers/{ProviderName}/.
public interface IPaymentProvider
{
    PaymentProviderCode Code { get; }
    string DisplayName { get; }

    /// Initialize a payment session with the provider (auth, create order, get payment URL).
    Task<ProviderInitResult> InitiateAsync(ProviderInitContext ctx, CancellationToken ct);

    /// Verify the signature on an inbound webhook and extract the canonical fields
    /// the orchestrator persists. Returns null + reason if verification fails.
    Task<WebhookVerifyResult> VerifyWebhookAsync(WebhookVerifyContext ctx, CancellationToken ct);
}

public record ProviderInitContext(
    CompanyProviderConfig Config,
    CompanyIntegrationMethod Method,
    PaymentOrder Order,
    Customer Customer);

public record ProviderInitResult(
    string ProviderOrderId,
    string PaymentToken,
    string RedirectUrl);

public record WebhookVerifyContext(
    CompanyProviderConfig Config,
    string RawBody,
    IDictionary<string, string> Headers,
    IDictionary<string, string> Query);

public record WebhookVerifyResult(
    bool IsValid,
    string? HmacReceived,
    string? HmacComputed,
    string? ProviderTransactionId,
    string? ProviderOrderId,
    string? MerchantOrderReference,
    long AmountMinor,
    string Currency,
    string ProviderIntegrationId,
    bool IsSuccess,
    bool IsPending,
    bool IsRefund,
    bool IsVoid,
    bool Is3DSecure,
    string? SourceType,
    string? SourceData,
    string? ErrorMessage,
    string? Reason);
