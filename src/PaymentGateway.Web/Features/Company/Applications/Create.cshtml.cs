using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Infrastructure.Multitenancy;
using PaymentGateway.Web.Infrastructure.Persistence;

namespace PaymentGateway.Web.Features.Company.Applications;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    public CreateModel(AppDbContext db, ICurrentUser user) { _db = db; _user = user; }

    [BindProperty, Required, StringLength(50)] public string AppCode { get; set; } = "";
    [BindProperty, Required, StringLength(200)] public string Name { get; set; } = "";
    [BindProperty, Required, Url, StringLength(500)] public string SuccessReturnUrl { get; set; } = "";
    [BindProperty, Required, Url, StringLength(500)] public string FailureReturnUrl { get; set; } = "";
    [BindProperty, Url, StringLength(500)] public string? PendingReturnUrl { get; set; }
    [BindProperty, Url, StringLength(500)] public string? WebhookUrl { get; set; }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        if (_user.CompanyId is not Guid cid) return Forbid();

        if (_db.Applications.Any(a => a.AppCode == AppCode))
        {
            ModelState.AddModelError(nameof(AppCode), "App code already in use.");
            return Page();
        }

        var app = new CompanyApplication
        {
            CompanyId = cid,
            AppCode = AppCode.Trim(),
            Name = Name.Trim(),
            SuccessReturnUrl = SuccessReturnUrl.Trim(),
            FailureReturnUrl = FailureReturnUrl.Trim(),
            PendingReturnUrl = PendingReturnUrl,
            WebhookUrl = WebhookUrl,
            IsActive = true,
        };
        _db.Applications.Add(app);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Application created. Generate an API key next.";
        return RedirectToPage("Keys", new { id = app.Id });
    }
}
