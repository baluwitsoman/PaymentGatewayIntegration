using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Providers;
using PaymentGateway.Web.Infrastructure.Security;

namespace PaymentGateway.Web.Features.SuperAdmin.Companies;

public class ProviderModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ISecretProtector _protector;
    private readonly IPaymentProviderRegistry _registry;
    public ProviderModel(AppDbContext db, ISecretProtector protector, IPaymentProviderRegistry registry)
    { _db = db; _protector = protector; _registry = registry; }

    public string CompanyName { get; set; } = "";
    public string ProviderName { get; set; } = "";

    [BindProperty] public Guid CompanyId { get; set; }
    [BindProperty] public PaymentProviderCode ProviderCode { get; set; }
    [BindProperty, Required] public ProviderEnvironment Environment { get; set; } = ProviderEnvironment.Sandbox;
    [BindProperty] public string? ApiKey { get; set; }
    [BindProperty] public string? PublicKey { get; set; }
    [BindProperty] public string? SecretKey { get; set; }
    [BindProperty] public string? HmacSecret { get; set; }
    [BindProperty] public string? BaseUrl { get; set; }
    [BindProperty] public string? DisplayLabel { get; set; }
    [BindProperty] public string? ExtraConfigJson { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;
    [BindProperty] public int SortOrder { get; set; }
    public bool HasConfig { get; set; }
    public string WebhookUrl { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(Guid id, PaymentProviderCode code)
    {
        var c = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        if (!_registry.TryGet(code, out var prov)) return NotFound();

        CompanyName = c.Name;
        ProviderName = prov.DisplayName;
        CompanyId = id;
        ProviderCode = code;

        var cfg = await _db.ProviderConfigs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.CompanyId == id && x.ProviderCode == code);
        if (cfg != null)
        {
            HasConfig = true;
            Environment = cfg.Environment;
            BaseUrl = cfg.BaseUrl;
            DisplayLabel = cfg.DisplayLabel;
            ExtraConfigJson = cfg.ExtraConfigJson;
            IsActive = cfg.IsActive;
            SortOrder = cfg.SortOrder;
        }
        else
        {
            // Defaults now come from the editable provider catalog, not a hard-coded switch.
            var catalog = await _db.PaymentProviders.FirstOrDefaultAsync(p => p.Code == code);
            BaseUrl = catalog?.DefaultBaseUrl ?? "";
            ExtraConfigJson = catalog?.ExampleExtraConfigJson ?? "{}";
        }

        var orchBase = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
        WebhookUrl = $"{orchBase}/webhooks/{code}/{id}";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var c = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == CompanyId);
        if (c == null) return NotFound();
        if (!_registry.TryGet(ProviderCode, out _)) return NotFound();

        // Validate ExtraConfigJson is valid JSON if provided
        if (!string.IsNullOrWhiteSpace(ExtraConfigJson))
        {
            try { _ = JsonDocument.Parse(ExtraConfigJson); }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(ExtraConfigJson), $"Invalid JSON: {ex.Message}");
                await OnGetAsync(CompanyId, ProviderCode);
                return Page();
            }
        }

        var cfg = await _db.ProviderConfigs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.CompanyId == CompanyId && x.ProviderCode == ProviderCode);
        var isNew = cfg == null;
        cfg ??= new CompanyProviderConfig { CompanyId = CompanyId, ProviderCode = ProviderCode };

        cfg.Environment = Environment;
        cfg.BaseUrl = BaseUrl ?? "";
        cfg.DisplayLabel = DisplayLabel;
        cfg.ExtraConfigJson = ExtraConfigJson;
        cfg.IsActive = IsActive;
        cfg.SortOrder = SortOrder;

        if (!string.IsNullOrWhiteSpace(ApiKey)) cfg.ApiKeyEncrypted = _protector.Encrypt(ApiKey.Trim());
        if (!string.IsNullOrWhiteSpace(PublicKey)) cfg.PublicKeyEncrypted = _protector.Encrypt(PublicKey.Trim());
        if (!string.IsNullOrWhiteSpace(SecretKey)) cfg.SecretKeyEncrypted = _protector.Encrypt(SecretKey.Trim());
        if (!string.IsNullOrWhiteSpace(HmacSecret)) cfg.HmacSecretEncrypted = _protector.Encrypt(HmacSecret.Trim());

        if (isNew)
        {
            // For Paymob: either the legacy ApiKey OR the newer (SecretKey + PublicKey) pair must be present.
            // For other providers: ApiKey alone is fine (we'll generalise per-provider as we add them).
            var hasLegacyKey = cfg.ApiKeyEncrypted != null;
            var hasUnifiedPair = cfg.SecretKeyEncrypted != null && cfg.PublicKeyEncrypted != null;

            if (!hasLegacyKey && !hasUnifiedPair)
            {
                ModelState.AddModelError("",
                    "Provide either an API key (older Paymob accounts) OR a Secret key + Public key (newer Paymob accounts).");
                await OnGetAsync(CompanyId, ProviderCode);
                return Page();
            }
            if (cfg.HmacSecretEncrypted == null)
            {
                ModelState.AddModelError(nameof(HmacSecret), "HMAC secret is required.");
                await OnGetAsync(CompanyId, ProviderCode);
                return Page();
            }
            _db.ProviderConfigs.Add(cfg);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"{ProviderCode} configuration saved.";
        return RedirectToPage("Providers", new { id = CompanyId });
    }
}
