namespace PaymentGateway.Web.Domain.Entities;

public class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid AppUserId { get; set; }
    public byte[] TokenHash { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public AppUser User { get; set; } = default!;
}
