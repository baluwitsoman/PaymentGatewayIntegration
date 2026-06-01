using System.Security.Cryptography;
using System.Text;
using ERPMultiTenent.Application.Common.Models;
using ERPMultiTenent.Application.ERP.AmmProvisioning.Payment;
using ERPMultiTenent.WebUI.Filters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ERPMultiTenent.Application.Amm;

/// Receives server-to-server webhooks from the Payment Gateway orchestrator.
/// This is NOT a browser endpoint — it never returns a redirect, only 200/4xx.
///
/// URL configured in the orchestrator's Application:
///   https://<your-hrms>/api/AmmPaymentReceived/Webhook
[ApiController]
[ApiExceptionFilter]
[AllowAnonymous]                          // authenticated by HMAC, not by user cookie
[Route("/api/[controller]")]
public class AmmPaymentReceivedController : ControllerBase
{
    private readonly PaymentProcessor _processor;
    private readonly ILogger<AmmPaymentReceivedController> _logger;
    private readonly PaymentGatewayOptions _gwOptions;
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    public AmmPaymentReceivedController(
        PaymentProcessor processor,
        IOptions<PaymentGatewayOptions> gwOptions,
        ILogger<AmmPaymentReceivedController> logger)
    {
        _processor = processor;
        _gwOptions = gwOptions.Value;
        _logger = logger;
    }

    /// Verifies HMAC-SHA256 signature, parses, dispatches the UnifiedPaymentCommand
    /// for idempotent processing. Returns 200 on success so the orchestrator's
    /// outbox marks the message delivered.
    [HttpPost("Webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        // 1. Read raw body — needed verbatim for signature verification.
        string rawBody;
        using (var reader = new StreamReader(Request.Body))
        {
            rawBody = await reader.ReadToEndAsync(ct);
        }

        // 2. Verify HMAC-SHA256 signature header (X-Signature: hex).
        var sig = Request.Headers["X-Signature"].FirstOrDefault() ?? "";
        var secret = _gwOptions.WebhookSecret ?? "";
        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogError("PaymentGateway:WebhookSecret not configured — rejecting webhook");
            return Unauthorized();
        }
        if (!VerifySignature(rawBody, sig, secret))
        {
            _logger.LogWarning("Webhook signature mismatch. Body length={Len}", rawBody.Length);
            return Unauthorized();
        }

        // 3. Parse via the orchestrator handler so we can swap formats if needed later.
        UnifiedPaymentCommand command;
        try
        {
            command = await _processor.HandleAsync("paymentgateway", rawBody);
            command.RawData = rawBody;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse webhook body");
            return BadRequest();
        }

        // 4. Idempotent dispatch. The handler upserts AMM_PAYMENTS keyed by
        //    (invoice_id, order_reference) so duplicate deliveries are no-ops.
        try
        {
            var result = await Mediator.Send(command, ct);
            return result.Succeeded ? Ok() : StatusCode(500);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook handler threw for order {Ref}", command.OrderReference);
            // 5xx → orchestrator retries with backoff.
            return StatusCode(500);
        }
    }

    private static bool VerifySignature(string body, string sigHex, string secret)
    {
        var expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes(body));
        var expectedHex = Convert.ToHexString(expected).ToLowerInvariant();
        var receivedHex = (sigHex ?? "").ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedHex),
            Encoding.UTF8.GetBytes(receivedHex));
    }
}
