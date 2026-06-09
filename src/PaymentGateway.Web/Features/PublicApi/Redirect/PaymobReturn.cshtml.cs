using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;

namespace PaymentGateway.Web.Features.PublicApi.Redirect;

/// Paymob's "Transaction Response Callback" — the URL Paymob redirects the
/// customer's BROWSER to after they finish on its hosted page. Configured
/// ONCE per integration in the Paymob portal (not per-payment), so this
/// route must be reference-free; the order id comes back in the query string.
///
/// Paymob query params include: id, merchant_order_id, success, amount_cents,
/// currency, hmac, integration_id, etc. We trust nothing — we look up our
/// own DB by merchant_order_id and redirect to the tenant's success/failure URL.
///
/// Route: /pay/return?merchant_order_id=ORD-2026-...&hmac=...&success=true
public class PaymobReturnModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ILogger<PaymobReturnModel> _logger;

    public PaymobReturnModel(AppDbContext db, ILogger<PaymobReturnModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public string Reference { get; set; } = "";
    public string Status { get; set; } = "verifying";
    public string? RedirectTo { get; set; }

    public async Task<IActionResult> OnGetAsync(string? merchant_order_id, string? success, CancellationToken ct)
    {
        // Paymob uses snake_case query keys — bind via the parameter names above.
        if (string.IsNullOrWhiteSpace(merchant_order_id))
        {
            _logger.LogWarning("Paymob return callback hit without merchant_order_id. Query={Query}",
                Request.QueryString.Value);
            return BadRequest("merchant_order_id is required");
        }

        Reference = merchant_order_id;

        var order = await _db.PaymentOrders.IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.OrderReference == merchant_order_id, ct);
        if (order == null)
        {
            _logger.LogWarning("Paymob return: no PaymentOrder for merchant_order_id={Ref}", merchant_order_id);
            return NotFound();
        }

        _logger.LogInformation(
            "Paymob browser return: ref={Ref} order.status={Status} query.success={Success}",
            merchant_order_id, order.Status, success);

        // Map our order state → URL to send the customer to. The query string
        // `success` is UNTRUSTED — the authoritative truth lives in our DB,
        // updated by the server-to-server webhook (which may have arrived
        // before this browser landing, or may still be in flight).
        var (status, target) = order.Status switch
        {
            PaymentOrderStatus.Paid => ("paid", order.SuccessReturnUrl),
            PaymentOrderStatus.Failed => ("failed", order.FailureReturnUrl),
            PaymentOrderStatus.Cancelled => ("cancelled", order.FailureReturnUrl),
            PaymentOrderStatus.Expired => ("expired", order.FailureReturnUrl),
            _ => ("verifying", (string?)null),
        };

        Status = status;
        if (target != null)
        {
            var sep = target.Contains('?') ? '&' : '?';
            RedirectTo = $"{target}{sep}status={status}&ref={merchant_order_id}";
        }
        return Page();
    }
}
