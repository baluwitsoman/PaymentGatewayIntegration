using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Security;

namespace PaymentGateway.Web.Infrastructure.Providers.Paymob;

public class PaymobProvider : IPaymentProvider
{
    public PaymentProviderCode Code => PaymentProviderCode.Paymob;
    public string DisplayName => "Paymob";

    private readonly HttpClient _http;
    private readonly ISecretProtector _protector;
    private readonly ILogger<PaymobProvider> _logger;

    public PaymobProvider(HttpClient http, ISecretProtector protector, ILogger<PaymobProvider> logger)
    {
        _http = http;
        _protector = protector;
        _logger = logger;
    }

    public async Task<ProviderInitResult> InitiateAsync(ProviderInitContext ctx, CancellationToken ct)
    {
        var baseUrl = ctx.Config.BaseUrl.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl)) baseUrl = "https://accept.paymob.com";

        var apiKey = _protector.Decrypt(ctx.Config.ApiKeyEncrypted);

        // 1. auth
        var authRes = await _http.PostAsJsonAsync($"{baseUrl}/api/auth/tokens",
            new PaymobAuthRequest(apiKey), ct);
        authRes.EnsureSuccessStatusCode();
        var auth = (await authRes.Content.ReadFromJsonAsync<PaymobAuthResponse>(cancellationToken: ct))!;

        // 2. register order
        var orderRes = await _http.PostAsJsonAsync($"{baseUrl}/api/ecommerce/orders",
            new PaymobOrderRequest(
                AuthToken: auth.Token,
                DeliveryNeeded: false,
                AmountCents: ctx.Order.AmountMinor,
                Currency: ctx.Order.Currency,
                MerchantOrderId: ctx.Order.OrderReference,
                Items: Array.Empty<object>()), ct);
        orderRes.EnsureSuccessStatusCode();
        var paymobOrder = (await orderRes.Content.ReadFromJsonAsync<PaymobOrderResponse>(cancellationToken: ct))!;

        // 3. payment key
        var (first, last) = SplitName(ctx.Customer.FullName);
        var billing = new PaymobBillingData(
            FirstName: first, LastName: last,
            Email: ctx.Customer.Email ?? "na@na.com",
            PhoneNumber: ctx.Customer.MobileNumber ?? "+201000000000");

        if (!long.TryParse(ctx.Method.ProviderIntegrationId, out var integrationIdLong))
            throw new InvalidOperationException("Paymob integration_id must be numeric.");

        var keyRes = await _http.PostAsJsonAsync($"{baseUrl}/api/acceptance/payment_keys",
            new PaymobPaymentKeyRequest(
                AuthToken: auth.Token,
                AmountCents: ctx.Order.AmountMinor,
                ExpirationSeconds: Math.Max(60, (int)(ctx.Order.ExpiresAt - DateTime.UtcNow).TotalSeconds),
                OrderId: paymobOrder.Id,
                BillingData: billing,
                Currency: ctx.Order.Currency,
                IntegrationId: integrationIdLong), ct);
        keyRes.EnsureSuccessStatusCode();
        var key = (await keyRes.Content.ReadFromJsonAsync<PaymobPaymentKeyResponse>(cancellationToken: ct))!;

        // Iframe ID is provider-specific config, stored in ExtraConfigJson.
        var iframeId = ReadIframeId(ctx.Config.ExtraConfigJson);

        var redirectUrl = !string.IsNullOrWhiteSpace(iframeId)
            ? $"{baseUrl}/api/acceptance/iframes/{iframeId}?payment_token={key.Token}"
            : $"{baseUrl}/api/acceptance/post_pay?payment_token={key.Token}";

        return new ProviderInitResult(paymobOrder.Id.ToString(), key.Token, redirectUrl);
    }

    public Task<WebhookVerifyResult> VerifyWebhookAsync(WebhookVerifyContext ctx, CancellationToken ct)
    {
        var hmacReceived = ctx.Query.TryGetValue("hmac", out var h) ? h : "";

        JsonElement root;
        try { root = JsonDocument.Parse(ctx.RawBody).RootElement; }
        catch
        {
            return Task.FromResult(EmptyFailure(hmacReceived, null, "Invalid JSON"));
        }

        var fields = PaymobHmacFields.Extract(root);
        var hmacSecret = _protector.Decrypt(ctx.Config.HmacSecretEncrypted);

        var concatenated = string.Concat(fields.Values.Select(v => v ?? ""));
        var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(hmacSecret), Encoding.UTF8.GetBytes(concatenated));
        var computed = Convert.ToHexString(hash).ToLowerInvariant();
        var valid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes((hmacReceived ?? "").ToLowerInvariant()));

        if (!valid) return Task.FromResult(EmptyFailure(hmacReceived, computed, "HMAC mismatch"));

        var obj = root.TryGetProperty("obj", out var o) ? o : root;
        var paymobTxId = obj.GetProperty("id").GetInt64().ToString();
        var orderObj = obj.GetProperty("order");
        var paymobOrderId = orderObj.GetProperty("id").GetInt64().ToString();
        var merchantRef = orderObj.TryGetProperty("merchant_order_id", out var mo) ? mo.GetString() : null;
        var isSuccess = obj.GetProperty("success").GetBoolean();
        var isPending = obj.TryGetProperty("pending", out var p) && p.GetBoolean();
        var isRefunded = obj.TryGetProperty("is_refunded", out var rf) && rf.GetBoolean();
        var isVoided = obj.TryGetProperty("is_voided", out var v) && v.GetBoolean();
        var is3ds = obj.TryGetProperty("is_3d_secure", out var t3) && t3.GetBoolean();
        var amount = obj.GetProperty("amount_cents").GetInt64();
        var currency = obj.GetProperty("currency").GetString() ?? "EGP";
        var integrationId = obj.GetProperty("integration_id").GetRawText().Trim('"');
        var errorOccured = obj.TryGetProperty("error_occured", out var eo) && eo.GetBoolean();
        var sourceData = obj.TryGetProperty("source_data", out var sd) ? sd.GetRawText() : null;
        var sourceType = obj.TryGetProperty("source_data", out var sd2) && sd2.TryGetProperty("type", out var st)
            ? st.GetString() : null;
        var errorMessage = obj.TryGetProperty("data", out var d) && d.TryGetProperty("message", out var dm)
            ? dm.GetString() : null;

        return Task.FromResult(new WebhookVerifyResult(
            IsValid: true,
            HmacReceived: hmacReceived,
            HmacComputed: computed,
            ProviderTransactionId: paymobTxId,
            ProviderOrderId: paymobOrderId,
            MerchantOrderReference: merchantRef,
            AmountMinor: amount,
            Currency: currency,
            ProviderIntegrationId: integrationId,
            IsSuccess: isSuccess && !errorOccured,
            IsPending: isPending,
            IsRefund: isRefunded,
            IsVoid: isVoided,
            Is3DSecure: is3ds,
            SourceType: sourceType,
            SourceData: sourceData,
            ErrorMessage: errorMessage,
            Reason: null));
    }

    private static WebhookVerifyResult EmptyFailure(string? hmacReceived, string? computed, string reason)
        => new(false, hmacReceived, computed, null, null, null, 0, "", "",
            false, false, false, false, false, null, null, null, reason);

    private static string? ReadIframeId(string? extraConfigJson)
    {
        if (string.IsNullOrWhiteSpace(extraConfigJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(extraConfigJson);
            if (doc.RootElement.TryGetProperty("iframeId", out var v)) return v.GetString();
        }
        catch { }
        return null;
    }

    private static (string first, string last) SplitName(string fullName)
    {
        var trimmed = fullName.Trim();
        var idx = trimmed.IndexOf(' ');
        return idx < 0 ? (trimmed, "NA") : (trimmed[..idx], trimmed[(idx + 1)..]);
    }
}
