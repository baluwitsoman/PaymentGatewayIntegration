using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;

namespace PaymentGateway.Web.Features.SuperAdmin.Companies;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty] public Guid Id { get; set; }
    [BindProperty, Required, StringLength(200)] public string Name { get; set; } = "";
    [BindProperty, EmailAddress] public string? ContactEmail { get; set; }
    [BindProperty, StringLength(30)] public string? ContactPhone { get; set; }
    [BindProperty, Required] public CompanyStatus Status { get; set; }
    [BindProperty] public string? Notes { get; set; }
    public string CompCode { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var c = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        Id = c.Id;
        CompCode = c.CompCode;
        Name = c.Name;
        ContactEmail = c.ContactEmail;
        ContactPhone = c.ContactPhone;
        Status = c.Status;
        Notes = c.Notes;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var c = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == Id);
        if (c == null) return NotFound();
        c.Name = Name.Trim();
        c.ContactEmail = ContactEmail;
        c.ContactPhone = ContactPhone;
        c.Status = Status;
        c.Notes = Notes;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Company updated.";
        return RedirectToPage("Index");
    }
}
