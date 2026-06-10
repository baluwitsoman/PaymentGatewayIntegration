using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Providers;

namespace PaymentGateway.Web.Features.SuperAdmin.Providers;

/// Global provider catalog. SuperAdmin enables/disables (add/remove) providers
/// and edits their display metadata. Identity stays the PaymentProviderCode enum;
/// this only manages the payment_provider rows.
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPaymentProviderRegistry _registry;
    public IndexModel(AppDbContext db, IPaymentProviderRegistry registry) { _db = db; _registry = registry; }

    public IReadOnlyList<Row> Rows { get; set; } = Array.Empty<Row>();

    public record Row(PaymentProviderCode Code, string DisplayName, bool IsEnabled, bool HasImpl, int SortOrder, int MappedCompanies);

    public async Task<IActionResult> OnGetAsync()
    {
        var catalog = await _db.PaymentProviders.OrderBy(p => p.SortOrder).ToListAsync();
        var mapCounts = await _db.ProviderMappings.IgnoreQueryFilters()
            .GroupBy(m => m.ProviderCode)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        Rows = catalog.Select(p => new Row(
            p.Code,
            p.DisplayName,
            p.IsEnabled,
            _registry.TryGet(p.Code, out _),
            p.SortOrder,
            mapCounts.TryGetValue(p.Code, out var n) ? n : 0)).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(PaymentProviderCode code)
    {
        var p = await _db.PaymentProviders.FirstOrDefaultAsync(x => x.Code == code);
        if (p != null)
        {
            p.IsEnabled = !p.IsEnabled;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{p.DisplayName} {(p.IsEnabled ? "enabled" : "disabled")}.";
        }
        return RedirectToPage();
    }
}
