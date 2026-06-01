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

        Rows = _registry.All.Select(p =>
        {
            var cfg = configs.FirstOrDefault(x => x.ProviderCode == p.Code);
            return new Row(
                p.Code,
                p.DisplayName,
                cfg != null,
                cfg?.IsActive ?? false,
                cfg?.Environment.ToString(),
                counts.TryGetValue(p.Code, out var n) ? n : 0);
        }).OrderBy(r => r.Code).ToList();
        return Page();
    }
}
