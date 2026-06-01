# HRMS consumer sample

Drop these files into your HRMS ASP.NET Core MVC project. They turn your existing `PaymentTestController` into a working end-to-end test of the payment flow.

## What's in here

```
Services/
  PaymentGatewayClient.cs    — typed HttpClient + options + DTOs

Controllers/
  PaymentTestController.cs   — Index (form + POST), Success, Failure, StatusJson

Views/PaymentTest/
  Index.cshtml               — Bootstrap form with "Pay now" button
  Success.cshtml             — verifies status server-side, polls while pending
  Failure.cshtml             — explains failure / expired
```

## 1. Files to copy

Paste each file into the matching folder of your HRMS project, then rename the `YourHrms.*` namespaces to match yours.

## 2. `appsettings.json` (add this section)

```json
"PaymentGateway": {
  "BaseUrl": "https://localhost:5001",
  "ApiKey": "pg_test_PASTE_FROM_ORCHESTRATOR_UI_HERE",
  "WebhookSecret": "OPTIONAL_IF_YOU_RECEIVE_WEBHOOKS"
}
```

- `BaseUrl` — wherever your orchestrator runs.
- `ApiKey` — paste the full key shown **once** in the orchestrator after generation. Treat as a secret (use user-secrets / environment variables in non-dev environments).

## 3. `Program.cs` registration

```csharp
using YourHrms.Services;

builder.Services.Configure<PaymentGatewayOptions>(
    builder.Configuration.GetSection("PaymentGateway"));

builder.Services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>()
    .ConfigureHttpClient((sp, c) =>
    {
        // SSL cert validation can be disabled FOR DEV ONLY when the orchestrator
        // is running with the default ASP.NET dev cert:
    });

// For dev only — accept the orchestrator's dev SSL cert. Remove for production.
#if DEBUG
builder.Services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
#endif
```

> ⚠️ If you keep both `AddHttpClient<...>` blocks the last one wins. Use only one — the `#if DEBUG` one in dev, the plain one in production.

## 4. Configure the orchestrator's Application return URLs

In the orchestrator UI: **Company → Applications → Edit**, set:

| Field | Value |
|---|---|
| Success return URL | `https://your-hrms.example/PaymentTest/Success` |
| Failure return URL | `https://your-hrms.example/PaymentTest/Failure` |
| Pending return URL | `https://your-hrms.example/PaymentTest/Success` (it'll poll) |

The orchestrator appends `?status=paid&ref=ORD-…` automatically. Your `Success` action ignores the query-string status and re-checks via the API, which is the secure behaviour.

## 4b. Make sure the company has at least one provider configured

In the orchestrator UI: **Companies → [your company] → Providers**. Configure at least one provider (e.g. Paymob) with credentials, then add at least one **Method** under it. Without an enabled method, the API returns 422.

## 5. Test routing

1. Browse to `https://your-hrms.example/PaymentTest`
2. Fill the form, click **Pay now**
3. Your server POSTs to itself → calls the orchestrator → gets `paymentUrl` → 302 redirects you there
4. The orchestrator's `/pay/{ref}` page hands off to Paymob
5. Pay with a [Paymob test card](https://docs.paymob.com/v2/docs/test-cards) (e.g. `5123 4567 8901 2346`, exp `12/29`, CVC `123`)
6. You land back on `/PaymentTest/Success?status=paid&ref=ORD-...`
7. The view shows a spinner if the webhook hasn't arrived yet, then auto-refreshes to "Payment received"

## 6. Optional — webhook receiver

If you want server-to-server confirmation in addition to the redirect, set the **Webhook URL** in the Application settings to e.g. `https://your-hrms.example/api/payment-webhooks`, plus a long random **Webhook signing secret**. Then add this to your HRMS:

```csharp
// Controllers/PaymentWebhooksController.cs
[ApiController]
[Route("api/payment-webhooks")]
public class PaymentWebhooksController : ControllerBase
{
    private readonly IOptions<PaymentGatewayOptions> _opts;
    private readonly ILogger<PaymentWebhooksController> _logger;
    public PaymentWebhooksController(IOptions<PaymentGatewayOptions> opts, ILogger<PaymentWebhooksController> logger)
    { _opts = opts; _logger = logger; }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        using var sr = new StreamReader(Request.Body);
        var body = await sr.ReadToEndAsync(ct);

        var sigHeader = Request.Headers["X-Signature"].ToString();
        var secret = _opts.Value.WebhookSecret ?? "";
        if (!Verify(body, sigHeader, secret)) return Unauthorized();

        var evt = System.Text.Json.JsonSerializer.Deserialize<WebhookEvent>(body,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (evt is null) return BadRequest();

        // TODO: dedupe on (order_reference, event_type) and update your DB.
        _logger.LogInformation("Payment {Ref} → {Status} (tx {Tx})",
            evt.order_reference, evt.status, evt.paymob_transaction_id);

        return Ok();
    }

    private static bool Verify(string body, string sigHex, string secret)
    {
        var expected = System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(secret),
            System.Text.Encoding.UTF8.GetBytes(body));
        var expectedHex = Convert.ToHexString(expected).ToLowerInvariant();
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(expectedHex),
            System.Text.Encoding.UTF8.GetBytes((sigHex ?? "").ToLowerInvariant()));
    }

    public record WebhookEvent(string event_type, string order_reference, string? external_reference,
        string status, long amount_minor, string currency, DateTime? paid_at,
        string? provider, string? provider_transaction_id);
}
```
