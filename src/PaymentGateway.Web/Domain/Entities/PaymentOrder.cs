using PaymentGateway.Web.Domain.Enums;

namespace PaymentGateway.Web.Domain.Entities;

public class PaymentOrder
{
    public Guid Id { get; set; }
    public string OrderReference { get; set; } = default!;
    public Guid CompanyId { get; set; }
    public Guid CompanyApplicationId { get; set; }
    public Guid CustomerId { get; set; }
    public string? ExternalReference { get; set; }
    public string? Description { get; set; }

    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "EGP";

    public PaymentOrderStatus Status { get; set; } = PaymentOrderStatus.Created;

    /// Null until the customer (or the calling app) selects a provider+method.
    /// Once set + initiated, ProviderOrderId/PaymentToken/PaymentUrl are populated.
    public PaymentProviderCode? SelectedProviderCode { get; set; }
    public Guid? SelectedMethodId { get; set; }

    public string? ProviderOrderId { get; set; }
    public string? PaymentToken { get; set; }
    public string? PaymentUrl { get; set; }

    public string SuccessReturnUrl { get; set; } = default!;
    public string FailureReturnUrl { get; set; } = default!;

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public uint Xmin { get; set; }

    public Company Company { get; set; } = default!;
    public CompanyApplication Application { get; set; } = default!;
    public Customer Customer { get; set; } = default!;
    public CompanyIntegrationMethod? SelectedMethod { get; set; }
    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
}
