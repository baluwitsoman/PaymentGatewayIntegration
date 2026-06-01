# Program.cs / Startup.cs registration snippet

Add these to your HRMS host configuration:

```csharp
using ERPMultiTenent.Application.ERP.AmmProvisioning.Payment;
using ERPMultiTenent.Infrastructure.Repository.PPM;

// Strongly-typed options bound from "PaymentGateway" section in appsettings.json
builder.Services.Configure<PaymentGatewayOptions>(
    builder.Configuration.GetSection("PaymentGateway"));

// Typed HttpClient with X-Api-Key header pre-set
builder.Services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>()
#if DEBUG
    // DEV ONLY — accept the orchestrator's dev SSL cert. Remove for production.
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
#endif
    ;

// Payment parser + new small invoice repo
builder.Services.AddSingleton<PaymentProcessor>();
builder.Services.AddScoped<IAmmInvoiceRepo, AmmInvoiceRepo>();
```

## appsettings.json (you already have this)

```json
"PaymentGateway": {
  "BaseUrl": "https://bpos1.bizonsys.com/PaymentGateway/",
  "ApiKey": "pg_test_iifaoicjvoh4cwihudislhk7xw3fcq5vvfmclizlikccj7hm42da",
  "WebhookSecret": "PASTE-THE-SAME-LONG-RANDOM-STRING-YOU-SET-IN-THE-ORCHESTRATOR-APPLICATION-WEBHOOK-SECRET"
}
```

## Database changes (Oracle DDL)

```sql
-- store the orchestrator's order_reference on the invoice for reconciliation
ALTER TABLE AMM_INVOICES ADD AI_ORDER_REFERENCE VARCHAR2(50);
CREATE INDEX IDX_AMM_INVOICES_ORDER_REF ON AMM_INVOICES(AI_ORDER_REFERENCE);

-- track the order_reference + provider on each payment attempt
ALTER TABLE AMM_PAYMENTS ADD AP_ORDER_REFERENCE VARCHAR2(50);
ALTER TABLE AMM_PAYMENTS ADD AP_STATUS          VARCHAR2(20);
ALTER TABLE AMM_PAYMENTS ADD AP_PROVIDER        VARCHAR2(30);
ALTER TABLE AMM_PAYMENTS ADD AP_PROVIDER_TXN_ID VARCHAR2(100);

-- enforce idempotency at the DB level
CREATE UNIQUE INDEX UX_AMM_PAYMENTS_IDEMPOTENCY
    ON AMM_PAYMENTS(AP_INVOICE_ID, AP_ORDER_REFERENCE, AP_STATUS);
```

You'll also need to add the matching properties on the EF entities `AMM_INVOICES` and `AMM_PAYMENTS`:

```csharp
// AMM_INVOICES
public string? AI_ORDER_REFERENCE { get; set; }

// AMM_PAYMENTS
public string? AP_ORDER_REFERENCE { get; set; }
public string? AP_STATUS          { get; set; }
public string? AP_PROVIDER        { get; set; }
public string? AP_PROVIDER_TXN_ID { get; set; }
```

## Orchestrator side — set on the Application

In the orchestrator UI: **Company → Applications → Edit your app**:

| Field | Value |
|---|---|
| Webhook URL | `https://your-hrms.example/api/AmmPaymentReceived/Webhook` |
| Webhook signing secret | (same string you put in `PaymentGateway.WebhookSecret`) |
| Success return URL | `https://your-hrms.example/Payment/Success` |
| Failure return URL | `https://your-hrms.example/Payment/Failure` |
| Pending return URL | `https://your-hrms.example/Payment/Success` |

## Remove these old config keys (no longer used)

```json
"PaymentGateWayProvider1": "...",     // ← delete
"SubscriptionSuccessPage": "..."      // ← delete (replaced by /Payment/Success)
```
