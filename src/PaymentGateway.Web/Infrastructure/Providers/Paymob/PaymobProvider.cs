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
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymobProvider> _logger;

    public PaymobProvider(HttpClient http, ISecretProtector protector,
        IConfiguration configuration, ILogger<PaymobProvider> logger)
    {
        _http = http;
        _protector = protector;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ProviderInitResult> InitiateAsync(ProviderInitContext ctx, CancellationToken ct)
    {
        var baseUrl = ctx.Config.BaseUrl.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl)) baseUrl = "https://accept.paymob.com";

        _logger.LogInformation(
            "Paymob.Initiate START | base={Base} order={Ref} amountMinor={Amt} currency={Cur} integrationId={Iid}",
            baseUrl, ctx.Order.OrderReference, ctx.Order.AmountMinor, ctx.Order.Currency, ctx.Method.ProviderIntegrationId);

        // Paymob has two generations. Detect based on which credentials are present:
        //  - Unified Intention API (newer; Oman + most new accounts): Secret Key + Public Key.
        //  - Legacy "Accept" flow (older accounts): API Key.
        // The two are NOT interchangeable. Secret Key cannot be used as api_key.
        if (ctx.Config.SecretKeyEncrypted != null && ctx.Config.PublicKeyEncrypted != null)
        {
            _logger.LogInformation("Paymob.Initiate | using UNIFIED Intention API path");
            return await InitiateUnifiedAsync(ctx, baseUrl, ct);
        }

        _logger.LogInformation("Paymob.Initiate | using LEGACY api_key path");
        return await InitiateLegacyAsync(ctx, baseUrl, ct);
    }

    // ====================================================================
    // Unified Intention API (newer Paymob accounts)
    //   POST /v1/intention/   with   Authorization: Token <secret_key>
    //   → response has client_secret
    //   → redirect customer to /unifiedcheckout/?publicKey=...&clientSecret=...
    // ====================================================================
    private async Task<ProviderInitResult> InitiateUnifiedAsync(
        ProviderInitContext ctx, string baseUrl, CancellationToken ct)
    {
        var secretKey = _protector.Decrypt(ctx.Config.SecretKeyEncrypted!);
        var publicKey = _protector.Decrypt(ctx.Config.PublicKeyEncrypted!);

        if (!long.TryParse(ctx.Method.ProviderIntegrationId, out var integrationIdLong))
            throw new InvalidOperationException(
                $"Paymob integration_id must be numeric, got '{ctx.Method.ProviderIntegrationId}'. " +
                "Check Methods page — paste the numeric ID from the Paymob portal, not the display name.");

        var orchestratorBase = _configuration["Orchestrator:BaseUrl"]?.TrimEnd('/') ?? "";
        var notificationUrl = $"{orchestratorBase}/webhooks/Paymob/{ctx.Config.CompanyId}";
        var redirectionUrl = $"{orchestratorBase}/pay/return";

        var (first, last) = SplitName(ctx.Customer.FullName);

        var body = new
        {
            amount = ctx.Order.AmountMinor,
            currency = ctx.Order.Currency,
            payment_methods = new long[] { integrationIdLong },
            items = new[]
            {
                new
                {
                    name = ctx.Order.Description ?? ctx.Order.OrderReference,
                    amount = ctx.Order.AmountMinor,
                    description = ctx.Order.Description ?? ctx.Order.OrderReference,
                    quantity = 1
                }
            },
            billing_data = new
            {
                first_name = first,
                last_name = last,
                phone_number = ctx.Customer.MobileNumber ?? "+96898989782",
                email = ctx.Customer.Email ?? "na@na.com",
                country = "OM",
                city = "NA",
                street = "NA",
                building = "NA",
                floor = "NA",
                apartment = "NA",
                state = "NA",
                postal_code = "NA",
            },
            customer = new
            {
                first_name = first,
                last_name = last,
                email = ctx.Customer.Email ?? "balavrs@gmail.com",
                extras = new { customer_code = ctx.Customer.CustomerCode }
            },
            extras = new { merchant_order_id = ctx.Order.OrderReference },
            special_reference = ctx.Order.OrderReference,
            notification_url = notificationUrl,
            redirection_url = redirectionUrl,
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/intention/");
        req.Headers.TryAddWithoutValidation("Authorization", $"Token {secretKey}");
        req.Content = JsonContent.Create(body);

        var res = await _http.SendAsync(req, ct);
        var respBody = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("Paymob.Intention FAILED status={Status} body={Body}", (int)res.StatusCode, respBody);
            throw new InvalidOperationException($"Paymob intention failed: {(int)res.StatusCode} {respBody}");
        }

        using var doc = JsonDocument.Parse(respBody);
        var root = doc.RootElement;
        var intentionId = root.GetProperty("id").ValueKind == JsonValueKind.String
            ? root.GetProperty("id").GetString()!
            : root.GetProperty("id").ToString();
        var clientSecret = root.GetProperty("client_secret").GetString()
            ?? throw new InvalidOperationException("Paymob intention response missing client_secret");

        _logger.LogInformation("Paymob.Intention OK intentionId={Id} clientSecretLen={Len}",
            intentionId, clientSecret.Length);

        var redirectUrl = $"{baseUrl}/unifiedcheckout/?publicKey={Uri.EscapeDataString(publicKey)}&clientSecret={Uri.EscapeDataString(clientSecret)}";

        _logger.LogInformation(
            "Paymob.Initiate DONE (unified) | order={Ref} intentionId={Id} redirect={Redirect}",
            ctx.Order.OrderReference, intentionId, redirectUrl);

        return new ProviderInitResult(intentionId, clientSecret, redirectUrl);
    }

    // ====================================================================
    // Legacy api_key flow (older accounts that still have API Key in portal)
    //   POST /api/auth/tokens   → bearer token
    //   POST /api/ecommerce/orders   → paymob_order_id
    //   POST /api/acceptance/payment_keys   → payment_token
    //   → redirect to /api/acceptance/iframes/{iframeId}?payment_token=...
    // ====================================================================
    private async Task<ProviderInitResult> InitiateLegacyAsync(
        ProviderInitContext ctx, string baseUrl, CancellationToken ct)
    {
        if (ctx.Config.ApiKeyEncrypted == null)
            throw new InvalidOperationException(
                "Paymob: neither (SecretKey+PublicKey) nor ApiKey is configured. " +
                "Newer Paymob accounts use Secret/Public keys; older accounts use the API key. " +
                "Configure one path in the Provider config.");

        var apiKey = _protector.Decrypt(ctx.Config.ApiKeyEncrypted);

        // 1. auth
        var authRes = await _http.PostAsJsonAsync($"{baseUrl}/api/auth/tokens",
            new PaymobAuthRequest(apiKey), ct);
        if (!authRes.IsSuccessStatusCode)
        {
            var err = await authRes.Content.ReadAsStringAsync(ct);
            _logger.LogError("Paymob.Auth FAILED status={Status} body={Body}", (int)authRes.StatusCode, err);
            throw new InvalidOperationException($"Paymob auth failed: {(int)authRes.StatusCode} {err}");
        }
        var auth = (await authRes.Content.ReadFromJsonAsync<PaymobAuthResponse>(cancellationToken: ct))!;
        _logger.LogInformation("Paymob.Auth OK tokenLen={Len}", auth.Token.Length);

        // 2. register order
        var orderRes = await _http.PostAsJsonAsync($"{baseUrl}/api/ecommerce/orders",
            new PaymobOrderRequest(
                AuthToken: auth.Token,
                DeliveryNeeded: false,
                AmountCents: ctx.Order.AmountMinor,
                Currency: ctx.Order.Currency,
                MerchantOrderId: ctx.Order.OrderReference,
                Items: Array.Empty<object>()), ct);
        if (!orderRes.IsSuccessStatusCode)
        {
            var err = await orderRes.Content.ReadAsStringAsync(ct);
            _logger.LogError("Paymob.RegisterOrder FAILED status={Status} body={Body}", (int)orderRes.StatusCode, err);
            throw new InvalidOperationException($"Paymob register order failed: {(int)orderRes.StatusCode} {err}");
        }
        var paymobOrder = (await orderRes.Content.ReadFromJsonAsync<PaymobOrderResponse>(cancellationToken: ct))!;
        _logger.LogInformation("Paymob.RegisterOrder OK paymobOrderId={Id}", paymobOrder.Id);

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
        if (!keyRes.IsSuccessStatusCode)
        {
            var err = await keyRes.Content.ReadAsStringAsync(ct);
            _logger.LogError("Paymob.PaymentKey FAILED status={Status} body={Body}", (int)keyRes.StatusCode, err);
            throw new InvalidOperationException($"Paymob payment key failed: {(int)keyRes.StatusCode} {err}");
        }
        var key = (await keyRes.Content.ReadFromJsonAsync<PaymobPaymentKeyResponse>(cancellationToken: ct))!;
        _logger.LogInformation("Paymob.PaymentKey OK tokenLen={Len}", key.Token.Length);

        // Iframe ID is provider-specific config, stored in ExtraConfigJson.
        var iframeId = ReadIframeId(ctx.Config.ExtraConfigJson);

        var redirectUrl = !string.IsNullOrWhiteSpace(iframeId)
            ? $"{baseUrl}/api/acceptance/iframes/{iframeId}?payment_token={key.Token}"
            : $"{baseUrl}/api/acceptance/post_pay?payment_token={key.Token}";

        _logger.LogInformation(
            "Paymob.Initiate DONE | order={Ref} paymobOrderId={Id} redirect={Redirect}",
            ctx.Order.OrderReference, paymobOrder.Id, redirectUrl);

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
        var currency = obj.GetProperty("currency").GetString() ?? "OMR";
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
