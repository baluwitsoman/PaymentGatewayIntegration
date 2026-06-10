using PaymentGateway.Web.Domain.Enums;

namespace PaymentGateway.Web.Domain.Entities;

/// Which catalog providers a company is allowed to use. This is the "mapping"
/// step — assignment of a provider to a tenant — kept separate from credential
/// entry (CompanyProviderConfig). A provider can be mapped to a company before
/// anyone enters keys, and the credential screen only lists mapped+enabled rows.
///
/// One row per (Company, Provider); unique on that pair.
public class CompanyProviderMapping
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public PaymentProviderCode ProviderCode { get; set; }

    /// Per-company on/off for this provider. Disabling hides it from the
    /// credential list without deleting the mapping or any saved config.
    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Company Company { get; set; } = default!;
}
