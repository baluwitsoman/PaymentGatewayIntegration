using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Providers;

namespace PaymentGateway.Web.Features.SuperAdmin.Companies;

public class ProvidersModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPaymentProviderRegistry _registry;
    public ProvidersModel(AppDbContext db, IPaymentProviderRegistry registry) { _db = db; _registry = registry; }

    public string CompanyName { get; set; } = "";
    public Guid CompanyId { get; set; }
    public IReadOnlyList<Row> Rows { get; set; } = Array.Empty<Row>();

    public record Row(PaymentProviderCode Code, string Name, bool Configured, bool IsActive, string? Environment, int MethodCount);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var c = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        CompanyName = c.Name;
        CompanyId = c.Id;

        var configs = await _db.ProviderConfigs.IgnoreQueryFilters()
            .Where(p => p.CompanyId == id).ToListAsync();
        var counts = await _db.IntegrationMethods.IgnoreQueryFilters()
            .Where(m => m.CompanyId == id)
            .GroupBy(m => m.ProviderCode)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        // List is now driven by the per-company mapping ∩ enabled catalog ∩ a
        // registered adapter — not the full provider registry. SuperAdmin assigns
        // providers via "Map providers"; only those appear here for credentials.
        var mappings = await _db.ProviderMappings.IgnoreQueryFilters()
            .Where(m => m.CompanyId == id && m.IsEnabled).ToListAsync();
        var catalog = await _db.PaymentProviders.Where(p => p.IsEnabled)
            .ToDictionaryAsync(p => p.Code);

        Rows = mappings
            .Where(m => catalog.ContainsKey(m.ProviderCode) && _registry.TryGet(m.ProviderCode, out _))
            .Select(m =>
            {
                var cfg = configs.FirstOrDefault(x => x.ProviderCode == m.ProviderCode);
                var name = catalog[m.ProviderCode].DisplayName;
                return new Row(
                    m.ProviderCode,
                    name,
                    cfg != null,
                    cfg?.IsActive ?? false,
                    cfg?.Environment.ToString(),
                    counts.TryGetValue(m.ProviderCode, out var n) ? n : 0);
            }).OrderBy(r => r.Code).ToList();
        return Page();
    }
}
