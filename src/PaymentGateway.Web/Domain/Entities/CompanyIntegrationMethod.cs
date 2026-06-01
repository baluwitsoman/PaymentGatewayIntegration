using PaymentGateway.Web.Domain.Enums;

namespace PaymentGateway.Web.Domain.Entities;

/// A method available to a tenant under a specific provider. Examples:
///   (CompanyA, Paymob,     Card,         "12345")  → Paymob card integration #12345
///   (CompanyA, Paymob,     MobileWallet, "67890")  → Paymob wallet integration
///   (CompanyA, BankMuscat, Card,         "MERC-X") → BankMuscat hosted card
public class CompanyIntegrationMethod
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public PaymentProviderCode ProviderCode { get; set; }
    public PaymentMethodType MethodType { get; set; }
    public string ProviderIntegrationId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Company Company { get; set; } = default!;
}
