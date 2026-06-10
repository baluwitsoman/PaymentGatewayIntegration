using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Providers;

namespace PaymentGateway.Web.Features.SuperAdmin.Companies;

/// Map catalog providers to a company. Assignment is separate from credential
/// entry: a provider mapped here then appears on the company's Providers screen
/// for credentials. Only catalog-enabled providers with a registered adapter can
/// be mapped.
public class MapProvidersModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPaymentProviderRegistry _registry;
    public MapProvidersModel(AppDbContext db, IPaymentProviderRegistry registry) { _db = db; _registry = registry; }

    public string CompanyName { get; set; } = "";
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public List<short> Selected { get; set; } = new();

    public IReadOnlyList<Row> Rows { get; set; } = Array.Empty<Row>();
    public record Row(PaymentProviderCode Code, string DisplayName, bool Mapped, bool Configured);

    public async Task<IActionResult> OnGetAsync()
    {
        var c = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == Id);
        if (c == null) return NotFound();
        CompanyName = c.Name;

        var catalog = await _db.PaymentProviders.Where(p => p.IsEnabled)
            .OrderBy(p => p.SortOrder).ToListAsync();
        var maps = await _db.ProviderMappings.IgnoreQueryFilters()
            .Where(m => m.CompanyId == Id).ToDictionaryAsync(m => m.ProviderCode);
        var configured = await _db.ProviderConfigs.IgnoreQueryFilters()
            .Where(x => x.CompanyId == Id).Select(x => x.ProviderCode).ToListAsync();
        var configuredSet = configured.ToHashSet();

        Rows = catalog
            .Where(p => _registry.TryGet(p.Code, out _))
            .Select(p =>
            {
                var mapped = maps.TryGetValue(p.Code, out var m) && m.IsEnabled;
                return new Row(p.Code, p.DisplayName, mapped, configuredSet.Contains(p.Code));
            }).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var c = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == Id);
        if (c == null) return NotFound();

        // Candidate set = catalog-enabled providers that have an adapter.
        var candidates = await _db.PaymentProviders.Where(p => p.IsEnabled).ToListAsync();
        var maps = await _db.ProviderMappings.IgnoreQueryFilters()
            .Where(m => m.CompanyId == Id).ToListAsync();

        foreach (var p in candidates)
        {
            if (!_registry.TryGet(p.Code, out _)) continue;
            var wanted = Selected.Contains((short)p.Code);
            var existing = maps.FirstOrDefault(m => m.ProviderCode == p.Code);
            if (existing != null)
            {
                existing.IsEnabled = wanted;
            }
            else if (wanted)
            {
                _db.ProviderMappings.Add(new CompanyProviderMapping
                {
                    CompanyId = Id,
                    ProviderCode = p.Code,
                    IsEnabled = true,
                });
            }
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = "Provider mapping saved.";
        return RedirectToPage("Providers", new { id = Id });
    }
}
