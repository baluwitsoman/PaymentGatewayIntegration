using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;

namespace PaymentGateway.Web.Features.SuperAdmin.Companies;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty, Required, StringLength(50)]
    public string CompCode { get; set; } = "";

    [BindProperty, Required, StringLength(200)]
    public string Name { get; set; } = "";

    [BindProperty, EmailAddress]
    public string? ContactEmail { get; set; }

    [BindProperty, StringLength(30)]
    public string? ContactPhone { get; set; }

    [BindProperty, Required, StringLength(3, MinimumLength = 3)]
    public string DefaultCurrency { get; set; } = "EGP";

    [BindProperty]
    public string? Notes { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        if (_db.Companies.Any(c => c.CompCode == CompCode))
        {
            ModelState.AddModelError(nameof(CompCode), "Company code already in use.");
            return Page();
        }

        var company = new PaymentGateway.Web.Domain.Entities.Company
        {
            CompCode = CompCode.Trim(),
            Name = Name.Trim(),
            ContactEmail = ContactEmail,
            ContactPhone = ContactPhone,
            DefaultCurrency = DefaultCurrency.ToUpperInvariant(),
            Notes = Notes,
            Status = CompanyStatus.Active,
        };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Company {company.Name} created. Configure a payment provider to enable payments.";
        return RedirectToPage("Providers", new { id = company.Id });
    }
}
