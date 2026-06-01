namespace PaymentGateway.Web.Domain.Entities;

public class CompanyApplication
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string AppCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string SuccessReturnUrl { get; set; } = default!;
    public string FailureReturnUrl { get; set; } = default!;
    public string? PendingReturnUrl { get; set; }
    public string? WebhookUrl { get; set; }
    public byte[]? WebhookSecretEncrypted { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Company Company { get; set; } = default!;
    public ICollection<ApplicationApiKey> ApiKeys { get; set; } = new List<ApplicationApiKey>();
}
