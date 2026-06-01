using System.Net;

namespace PaymentGateway.Web.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? AppUserId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Action { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public IPAddress? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}
