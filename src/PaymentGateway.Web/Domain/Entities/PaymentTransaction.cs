using PaymentGateway.Web.Domain.Enums;

namespace PaymentGateway.Web.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid PaymentOrderId { get; set; }
    public Guid CompanyId { get; set; }

    public PaymentProviderCode ProviderCode { get; set; }
    /// Provider-side transaction identifier as a string (Paymob uses long,
    /// BankMuscat uses GUID-ish strings; string is the safe lowest common denominator).
    public string ProviderTransactionId { get; set; } = default!;

    public bool IsSuccess { get; set; }
    public bool IsPending { get; set; }
    public bool IsRefund { get; set; }
    public bool IsVoid { get; set; }
    public bool Is3DSecure { get; set; }

    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "OMR";

    public string ProviderIntegrationId { get; set; } = default!;
    public string? SourceType { get; set; }
    public string? SourceData { get; set; }

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public bool HmacValid { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime ProcessedAt { get; set; }

    public PaymentOrder Order { get; set; } = default!;
}
