using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Providers;

namespace PaymentGateway.Web.Features.SuperAdmin.Companies;

public class MethodsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPaymentProviderRegistry _registry;
    public MethodsModel(AppDbContext db, IPaymentProviderRegistry registry) { _db = db; _registry = registry; }

    public string CompanyName { get; set; } = "";
    public string ProviderName { get; set; } = "";

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty(SupportsGet = true)] public PaymentProviderCode ProviderCode { get; set; }

    public IReadOnlyList<CompanyIntegrationMethod> Items { get; set; } = Array.Empty<CompanyIntegrationMethod>();

    [BindProperty] public Input New { get; set; } = new();

    public class Input
    {
        [Required] public PaymentMethodType MethodType { get; set; }
        [Required, StringLength(100)] public string ProviderIntegrationId { get; set; } = "";
        [Required, StringLength(100)] public string DisplayName { get; set; } = "";
        public int SortOrder { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var c = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == Id);
        if (c == null) return NotFound();
        if (!_registry.TryGet(ProviderCode, out var prov)) return NotFound();
        CompanyName = c.Name;
        ProviderName = prov.DisplayName;
        Items = await _db.IntegrationMethods.IgnoreQueryFilters()
            .Where(m => m.CompanyId == Id && m.ProviderCode == ProviderCode)
            .OrderBy(m => m.SortOrder).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ModelState.IsValid) return await OnGetAsync();
        _db.IntegrationMethods.Add(new CompanyIntegrationMethod
        {
            CompanyId = Id,
            ProviderCode = ProviderCode,
            MethodType = New.MethodType,
            ProviderIntegrationId = New.ProviderIntegrationId.Trim(),
            DisplayName = New.DisplayName.Trim(),
            SortOrder = New.SortOrder,
            IsEnabled = true,
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Method added.";
        return RedirectToPage(new { id = Id, providerCode = ProviderCode });
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid methodId)
    {
        var m = await _db.IntegrationMethods.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == methodId);
        if (m != null) { m.IsEnabled = !m.IsEnabled; await _db.SaveChangesAsync(); }
        return RedirectToPage(new { id = Id, providerCode = ProviderCode });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid methodId)
    {
        var m = await _db.IntegrationMethods.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == methodId);
        if (m != null) { _db.IntegrationMethods.Remove(m); await _db.SaveChangesAsync(); }
        return RedirectToPage(new { id = Id, providerCode = ProviderCode });
    }
}
