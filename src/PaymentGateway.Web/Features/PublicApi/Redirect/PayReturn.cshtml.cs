using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;

namespace PaymentGateway.Web.Features.PublicApi.Redirect;

/// Customer's browser lands here after the Paymob hosted page. The query
/// string is UNTRUSTED — we never persist payment status from it. We just
/// look up our own record (already updated by the server-to-server webhook
/// if it arrived) and redirect to the tenant app accordingly. If the webhook
/// hasn't arrived yet, we show a "verifying" page that polls.
public class PayReturnModel : PageModel
{
    private readonly AppDbContext _db;
    public PayReturnModel(AppDbContext db) => _db = db;

    public string Reference { get; set; } = "";
    public string Status { get; set; } = "verifying";
    public string? RedirectTo { get; set; }

    public async Task<IActionResult> OnGetAsync(string reference, CancellationToken ct)
    {
        Reference = reference;
        var order = await _db.PaymentOrders.IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.OrderReference == reference, ct);
        if (order == null) return NotFound();

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
            RedirectTo = $"{target}{sep}status={status}&ref={reference}";
        }
        return Page();
    }
}
