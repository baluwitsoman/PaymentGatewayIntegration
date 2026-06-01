using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Security;

namespace PaymentGateway.Web.Features.Company.Applications;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ISecretProtector _protector;
    public EditModel(AppDbContext db, ISecretProtector protector) { _db = db; _protector = protector; }

    [BindProperty] public Guid Id { get; set; }
    [BindProperty, Required] public string Name { get; set; } = "";
    [BindProperty, Required, Url] public string SuccessReturnUrl { get; set; } = "";
    [BindProperty, Required, Url] public string FailureReturnUrl { get; set; } = "";
    [BindProperty, Url] public string? PendingReturnUrl { get; set; }
    [BindProperty, Url] public string? WebhookUrl { get; set; }
    /// Optional override — paste your own secret if you have one in a secrets manager.
    /// Most users should click "Generate" instead.
    [BindProperty] public string? WebhookSecret { get; set; }
    [BindProperty] public bool IsActive { get; set; }
    public string AppCode { get; set; } = "";
    public bool HasWebhookSecret { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var a = await _db.Applications.FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        Id = a.Id;
        AppCode = a.AppCode;
        Name = a.Name;
        SuccessReturnUrl = a.SuccessReturnUrl;
        FailureReturnUrl = a.FailureReturnUrl;
        PendingReturnUrl = a.PendingReturnUrl;
        WebhookUrl = a.WebhookUrl;
        HasWebhookSecret = a.WebhookSecretEncrypted != null;
        IsActive = a.IsActive;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var a = await _db.Applications.FirstOrDefaultAsync(x => x.Id == Id);
        if (a == null) return NotFound();
        a.Name = Name.Trim();
        a.SuccessReturnUrl = SuccessReturnUrl.Trim();
        a.FailureReturnUrl = FailureReturnUrl.Trim();
        a.PendingReturnUrl = PendingReturnUrl;
        a.WebhookUrl = WebhookUrl;
        a.IsActive = IsActive;

        if (!string.IsNullOrWhiteSpace(WebhookSecret))
            a.WebhookSecretEncrypted = _protector.Encrypt(WebhookSecret.Trim());

        // Refuse to save a WebhookUrl without a secret — that would create the
        // very inconsistency we just fixed in the outbox.
        if (!string.IsNullOrWhiteSpace(a.WebhookUrl) && a.WebhookSecretEncrypted == null)
        {
            ModelState.AddModelError(nameof(WebhookSecret),
                "A webhook URL requires a webhook secret. Click \"Generate\" or paste one.");
            HasWebhookSecret = false;
            return Page();
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Application updated.";
        return RedirectToPage("Index");
    }

    /// Generates a cryptographically strong webhook secret, encrypts it, saves it,
    /// and surfaces the plaintext to the page ONCE via TempData. Same pattern as
    /// the API-key flow on Keys.cshtml.
    public async Task<IActionResult> OnPostGenerateSecretAsync()
    {
        var a = await _db.Applications.FirstOrDefaultAsync(x => x.Id == Id);
        if (a == null) return NotFound();

        var secret = GenerateSecret();
        a.WebhookSecretEncrypted = _protector.Encrypt(secret);
        await _db.SaveChangesAsync();

        TempData["NewWebhookSecret"] = secret;
        TempData["Success"] = "New webhook secret generated. Copy it now — it won't be shown again.";
        return RedirectToPage(new { id = Id });
    }

    private static string GenerateSecret()
    {
        // 32 bytes → 256 bits of entropy. Base32-encoded (lowercase) for safe copy/paste.
        var bytes = RandomNumberGenerator.GetBytes(32);
        const string alphabet = "abcdefghijklmnopqrstuvwxyz234567";
        var sb = new System.Text.StringBuilder("whsec_");
        int buffer = 0, bits = 0;
        foreach (var b in bytes)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                sb.Append(alphabet[(buffer >> bits) & 0x1F]);
            }
        }
        if (bits > 0) sb.Append(alphabet[(buffer << (5 - bits)) & 0x1F]);
        return sb.ToString();
    }
}
