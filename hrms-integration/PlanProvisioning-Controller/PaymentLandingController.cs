using ERPMultiTenent.Application.ERP.AmmProvisioning.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPMultiTenent.Application.Amm;

/// Where the customer's browser lands after paying (or failing) on the provider.
/// The orchestrator appends ?status=...&ref=ORD-... to whatever URL you set as
/// the Application's Success / Failure / Pending return URL.
///
/// Routes:
///   GET  /Payment/Success?ref=ORD-...&status=paid
///   GET  /Payment/Failure?ref=ORD-...&status=failed|expired|cancelled
///   GET  /Payment/StatusJson?ref=ORD-...   (used by the success page to poll
///                                           while waiting for the webhook race)
///
/// Notes
///  • [AllowAnonymous] — the user's auth cookie MAY have been lost in the
///    cross-site round-trip. We don't gate on it; the page renders for anyone
///    with a valid `ref`. Personal greeting only shown when User.IsAuthenticated.
///  • Never trust the `?status=` query string for crediting — re-fetch from the
///    orchestrator (server-to-server, authenticated by API key).
///  • The webhook may not have arrived yet when the browser lands. The view
///    polls /StatusJson until the order is terminal.
[Route("Payment")]
[AllowAnonymous]
public class PaymentLandingController : Controller
{
    private readonly IPaymentGatewayClient _gateway;
    private readonly IAmmInvoiceRepo _invoices;
    private readonly ILogger<PaymentLandingController> _logger;

    public PaymentLandingController(
        IPaymentGatewayClient gateway,
        IAmmInvoiceRepo invoices,
        ILogger<PaymentLandingController> logger)
    {
        _gateway = gateway;
        _invoices = invoices;
        _logger = logger;
    }

    [HttpGet("Success")]
    public async Task<IActionResult> Success(string @ref, string? status, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(@ref))
            return RedirectToAction("Index", "Home");

        var vm = await BuildResultViewModelAsync(@ref, status ?? "verifying", ct);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpGet("Failure")]
    public async Task<IActionResult> Failure(string @ref, string? status, CancellationToken ct)
    {
        var vm = await BuildResultViewModelAsync(@ref ?? "", status ?? "failed", ct);
        if (vm is null) vm = new PaymentResultVm { OrderReference = @ref ?? "", Status = status ?? "failed" };
        return View(vm);
    }

    /// Polled by the Success view while status is not yet terminal.
    [HttpGet("StatusJson")]
    public async Task<IActionResult> StatusJson(string @ref, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(@ref)) return BadRequest();
        var s = await _gateway.GetStatusAsync(@ref, ct);
        if (s == null) return NotFound();
        return Json(new
        {
            status = s.Status,
            paidAt = s.PaidAt,
            txId = s.LastTransactionId,
            provider = s.Provider
        });
    }

    private async Task<PaymentResultVm?> BuildResultViewModelAsync(string orderRef, string browserStatus, CancellationToken ct)
    {
        // 1. Local lookup — what does our DB say?
        var invoice = await _invoices.FindByOrderReferenceAsync(orderRef, ct);

        // 2. Authoritative re-fetch from the orchestrator (untrust the query string).
        var status = await _gateway.GetStatusAsync(orderRef, ct);

        if (invoice == null && status == null) return null;

        var resolvedStatus = status?.Status
            ?? (invoice?.FullyPaid == "Y" ? "Paid" : "Pending");

        return new PaymentResultVm
        {
            OrderReference = orderRef,
            InvoiceId = invoice?.InvoiceId,
            Status = resolvedStatus,
            AmountPaid = status != null ? status.AmountMinor / 100m : invoice?.Amount ?? 0m,
            Currency = status?.Currency ?? "OMR",
            PaidAt = status?.PaidAt,
            Provider = status?.Provider,
            ProviderTransactionId = status?.LastTransactionId,
            IsLoggedIn = User?.Identity?.IsAuthenticated == true,
            UserDisplayName = User?.Identity?.Name,
        };
    }
}

public class PaymentResultVm
{
    public string OrderReference { get; set; } = "";
    public int? InvoiceId { get; set; }
    public string Status { get; set; } = "Pending";
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "OMR";
    public DateTime? PaidAt { get; set; }
    public string? Provider { get; set; }
    public string? ProviderTransactionId { get; set; }
    public bool IsLoggedIn { get; set; }
    public string? UserDisplayName { get; set; }
}
