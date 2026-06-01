using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ERPMultiTenent.Application.ERP.AmmProvisioning.Payment;

/// Configuration section in appsettings.json:
///   "PaymentGateway": {
///     "BaseUrl": "https://bpos1.bizonsys.com/PaymentGateway/",
///     "ApiKey":  "pg_test_xxx",
///     "WebhookSecret": "long-random-shared-with-orchestrator"
///   }
public class PaymentGatewayOptions
{
    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string? WebhookSecret { get; set; }
}

public interface IPaymentGatewayClient
{
    Task<CreatePaymentGwResponse> CreatePaymentAsync(CreatePaymentGwRequest req, CancellationToken ct = default);
    Task<PaymentStatusGwResponse?> GetStatusAsync(string orderReference, CancellationToken ct = default);
}

public record CreatePaymentGwRequest(
    string CustomerCode,
    string CustomerName,
    string? MobileNumber,
    string? Email,
    long AmountMinor,
    string? Currency,
    string? Description,
    string? ExternalReference,
    string? PreferredProvider = null,
    string? PreferredMethod = null,
    Dictionary<string, object>? Metadata = null);

public record CreatePaymentGwResponse(
    Guid PaymentOrderId,
    string OrderReference,
    string Status,
    string PaymentUrl,
    DateTime ExpiresAt);

public record PaymentStatusGwResponse(
    Guid PaymentOrderId,
    string OrderReference,
    string Status,
    long AmountMinor,
    string Currency,
    DateTime CreatedAt,
    DateTime? PaidAt,
    string? ExternalReference,
    string? Provider,
    string? LastTransactionId);

public class PaymentGatewayClient : IPaymentGatewayClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public PaymentGatewayClient(HttpClient http, IOptions<PaymentGatewayOptions> options)
    {
        _http = http;
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.BaseUrl)) throw new InvalidOperationException("PaymentGateway:BaseUrl not configured");
        if (string.IsNullOrWhiteSpace(opts.ApiKey)) throw new InvalidOperationException("PaymentGateway:ApiKey not configured");

        _http.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Add("X-Api-Key", opts.ApiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<CreatePaymentGwResponse> CreatePaymentAsync(CreatePaymentGwRequest req, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync("api/v1/payments", req, JsonOpts, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"PaymentGateway POST failed {(int)res.StatusCode}: {body}");
        }
        return (await res.Content.ReadFromJsonAsync<CreatePaymentGwResponse>(JsonOpts, ct))!;
    }

    public async Task<PaymentStatusGwResponse?> GetStatusAsync(string orderReference, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/v1/payments/{Uri.EscapeDataString(orderReference)}", ct);
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<PaymentStatusGwResponse>(JsonOpts, ct);
    }
}
