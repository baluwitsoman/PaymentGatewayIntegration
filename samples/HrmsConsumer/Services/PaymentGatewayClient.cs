// File: Services/PaymentGatewayClient.cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace YourHrms.Services;

public class PaymentGatewayOptions
{
    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string? WebhookSecret { get; set; }
}

public interface IPaymentGatewayClient
{
    Task<CreatePaymentResponse> CreateAsync(CreatePaymentRequest req, CancellationToken ct = default);
    Task<PaymentStatusResponse?> GetStatusAsync(string orderReference, CancellationToken ct = default);
}

public record CreatePaymentRequest(
    string CustomerCode,
    string CustomerName,
    string? MobileNumber,
    string? Email,
    long AmountMinor,
    string? Currency,
    string? Description,
    string? ExternalReference,
    /// "Paymob" / "BankMuscat" / "NBO". Null → customer chooses on orchestrator's page.
    string? PreferredProvider = null,
    /// Honoured only when PreferredProvider is also set. "Card", "MobileWallet", "Installments", "Kiosk", "Cash".
    string? PreferredMethod = null,
    Dictionary<string, object>? Metadata = null);

public record CreatePaymentResponse(
    Guid PaymentOrderId,
    string OrderReference,
    string Status,
    string PaymentUrl,
    DateTime ExpiresAt);

public record PaymentStatusResponse(
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
    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public PaymentGatewayClient(HttpClient http, Microsoft.Extensions.Options.IOptions<PaymentGatewayOptions> options)
    {
        _http = http;
        _http.BaseAddress = new Uri(options.Value.BaseUrl);
        _http.DefaultRequestHeaders.Add("X-Api-Key", options.Value.ApiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<CreatePaymentResponse> CreateAsync(CreatePaymentRequest req, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync("/api/v1/payments", req, JsonOpts, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Payment gateway error {(int)res.StatusCode}: {body}");
        }
        return (await res.Content.ReadFromJsonAsync<CreatePaymentResponse>(JsonOpts, ct))!;
    }

    public async Task<PaymentStatusResponse?> GetStatusAsync(string orderReference, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"/api/v1/payments/{orderReference}", ct);
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<PaymentStatusResponse>(JsonOpts, ct);
    }
}
