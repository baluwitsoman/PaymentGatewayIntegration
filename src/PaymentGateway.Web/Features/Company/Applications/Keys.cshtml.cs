using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Security;

namespace PaymentGateway.Web.Features.Company.Applications;

public class KeysModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IApiKeyGenerator _gen;
    public KeysModel(AppDbContext db, IApiKeyGenerator gen) { _db = db; _gen = gen; }

    public CompanyApplication App { get; set; } = default!;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty, StringLength(100)] public string? Label { get; set; }
    public string? NewKeyShownOnce { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        App = (await _db.Applications.Include(a => a.ApiKeys).FirstOrDefaultAsync(a => a.Id == Id))!;
        if (App == null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        var companyId = _db.Applications.Where(a => a.Id == Id).Select(a => a.CompanyId).First();
        // Use any live provider config to decide the env tag, else default to test.
        var anyLive = await _db.ProviderConfigs.AnyAsync(p => p.CompanyId == companyId && p.Environment == ProviderEnvironment.Live && p.IsActive);
        var envTag = anyLive ? "live" : "test";

        var (fullKey, prefix, hash) = _gen.Generate(envTag);
        _db.ApplicationApiKeys.Add(new ApplicationApiKey
        {
            CompanyApplicationId = Id,
            KeyPrefix = prefix,
            KeyHash = hash,
            Label = Label,
            IsActive = true,
        });
        await _db.SaveChangesAsync();
        TempData["NewKey"] = fullKey;
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid keyId)
    {
        var k = await _db.ApplicationApiKeys.FirstOrDefaultAsync(x => x.Id == keyId);
        if (k != null && k.RevokedAt == null)
        {
            k.IsActive = false;
            k.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id = Id });
    }
}
