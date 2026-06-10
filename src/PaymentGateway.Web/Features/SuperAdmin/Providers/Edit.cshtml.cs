using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;

namespace PaymentGateway.Web.Features.SuperAdmin.Providers;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) { _db = db; }

    [BindProperty(SupportsGet = true)] public PaymentProviderCode Code { get; set; }
    [BindProperty, Required, StringLength(100)] public string DisplayName { get; set; } = "";
    [BindProperty] public bool IsEnabled { get; set; }
    [BindProperty, StringLength(200)] public string? DefaultBaseUrl { get; set; }
    [BindProperty] public string? ExampleExtraConfigJson { get; set; }
    [BindProperty] public int SortOrder { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var p = await _db.PaymentProviders.FirstOrDefaultAsync(x => x.Code == Code);
        if (p == null) return NotFound();
        DisplayName = p.DisplayName;
        IsEnabled = p.IsEnabled;
        DefaultBaseUrl = p.DefaultBaseUrl;
        ExampleExtraConfigJson = p.ExampleExtraConfigJson;
        SortOrder = p.SortOrder;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var p = await _db.PaymentProviders.FirstOrDefaultAsync(x => x.Code == Code);
        if (p == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(ExampleExtraConfigJson))
        {
            try { _ = JsonDocument.Parse(ExampleExtraConfigJson); }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(ExampleExtraConfigJson), $"Invalid JSON: {ex.Message}");
                return Page();
            }
        }
        if (!ModelState.IsValid) return Page();

        p.DisplayName = DisplayName.Trim();
        p.IsEnabled = IsEnabled;
        p.DefaultBaseUrl = string.IsNullOrWhiteSpace(DefaultBaseUrl) ? null : DefaultBaseUrl.Trim();
        p.ExampleExtraConfigJson = string.IsNullOrWhiteSpace(ExampleExtraConfigJson) ? null : ExampleExtraConfigJson.Trim();
        p.SortOrder = SortOrder;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"{p.DisplayName} updated.";
        return RedirectToPage("Index");
    }
}
