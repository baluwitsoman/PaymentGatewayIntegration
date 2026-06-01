using PaymentGateway.Web.Domain.Enums;

namespace PaymentGateway.Web.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = default!;
    public Guid CompanyId { get; set; }
    public Guid CompanyApplicationId { get; set; }
    public Guid PaymentOrderId { get; set; }
    public string TargetUrl { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string? Signature { get; set; }

    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 8;
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? LastAttemptedAt { get; set; }
    public int? LastResponseCode { get; set; }
    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
