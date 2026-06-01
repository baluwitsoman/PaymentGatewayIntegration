using PaymentGateway.Web.Domain.Enums;

namespace PaymentGateway.Web.Domain.Entities;

/// One row per (Company, Provider). Holds the encrypted credentials for that
/// tenant against that specific provider. A tenant may have rows for several
/// providers and the customer chooses which one to use at payment time.
public class CompanyProviderConfig
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public PaymentProviderCode ProviderCode { get; set; }

    public ProviderEnvironment Environment { get; set; } = ProviderEnvironment.Sandbox;

    /// Primary API credential. Most providers have this; named generically.
    public byte[] ApiKeyEncrypted { get; set; } = default!;
    public byte[]? PublicKeyEncrypted { get; set; }
    public byte[]? SecretKeyEncrypted { get; set; }
    public byte[] HmacSecretEncrypted { get; set; } = default!;

    /// Provider-specific knobs that don't fit the generic fields above
    /// (e.g. Paymob iframe_id, BankMuscat merchant_id, NBO terminal_id).
    /// Stored as JSON in `extra_config_json`. Each provider's IPaymentProvider
    /// impl knows how to read its own keys.
    public string? ExtraConfigJson { get; set; }

    public string BaseUrl { get; set; } = "";
    public string? DisplayLabel { get; set; }       // optional customer-facing override
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Company Company { get; set; } = default!;
}
