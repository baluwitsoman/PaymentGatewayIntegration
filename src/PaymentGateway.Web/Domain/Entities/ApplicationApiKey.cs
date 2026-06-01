namespace PaymentGateway.Web.Domain.Entities;

public class ApplicationApiKey
{
    public Guid Id { get; set; }
    public Guid CompanyApplicationId { get; set; }
    public string KeyPrefix { get; set; } = default!;
    public byte[] KeyHash { get; set; } = default!;
    public string? Label { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public CompanyApplication Application { get; set; } = default!;
}
