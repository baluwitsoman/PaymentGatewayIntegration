using PaymentGateway.Web.Domain.Enums;

namespace PaymentGateway.Web.Domain.Entities;

public class Company
{
    public Guid Id { get; set; }
    public string CompCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public CompanyStatus Status { get; set; } = CompanyStatus.Active;
    public string DefaultCurrency { get; set; } = "EGP";
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<CompanyProviderConfig> Providers { get; set; } = new List<CompanyProviderConfig>();
    public ICollection<CompanyIntegrationMethod> IntegrationMethods { get; set; } = new List<CompanyIntegrationMethod>();
    public ICollection<CompanyApplication> Applications { get; set; } = new List<CompanyApplication>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
