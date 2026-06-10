using PaymentGateway.Web.Domain.Enums;

namespace PaymentGateway.Web.Domain.Entities;

/// Global catalog of payment providers the platform can offer. One row per
/// PaymentProviderCode. The enum remains the canonical identity / FK used across
/// orders, transactions, methods and configs — this table only holds the editable
/// metadata (display name, enabled flag, default URL, example config) so SuperAdmin
/// can add/remove and tune providers without a code change or migration.
///
/// A provider is only usable when ALL hold:
///   1. an IPaymentProvider impl is registered in DI (the integration adapter), AND
///   2. this catalog row has IsEnabled = true, AND
///   3. the company has an enabled CompanyProviderMapping row.
public class PaymentProvider
{
    /// Primary key == the PaymentProviderCode enum value. Never renumbered.
    public PaymentProviderCode Code { get; set; }

    public string DisplayName { get; set; } = "";

    /// Global on/off. "Remove" from the catalog == set false (soft); we never
    /// hard-delete because historical orders/transactions reference the code.
    public bool IsEnabled { get; set; } = true;

    /// Seed value pre-filled into the credential form when a company first
    /// configures this provider. Replaces the old hard-coded switch.
    public string? DefaultBaseUrl { get; set; }
    public string? ExampleExtraConfigJson { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
