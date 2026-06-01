using System.Net;
using PaymentGateway.Web.Domain.Enums;

namespace PaymentGateway.Web.Domain.Entities;

public class WebhookEvent
{
    public Guid Id { get; set; }
    public WebhookSource Source { get; set; }
    public PaymentProviderCode? ProviderCode { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PaymentOrderId { get; set; }
    public string? ProviderTransactionId { get; set; }

    public string Headers { get; set; } = "{}";
    public string Body { get; set; } = "{}";

    public string? HmacReceived { get; set; }
    public string? HmacComputed { get; set; }
    public bool HmacValid { get; set; }

    public WebhookProcessingStatus ProcessingStatus { get; set; } = WebhookProcessingStatus.Pending;
    public string? ProcessingError { get; set; }

    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public IPAddress? RemoteIp { get; set; }
}
