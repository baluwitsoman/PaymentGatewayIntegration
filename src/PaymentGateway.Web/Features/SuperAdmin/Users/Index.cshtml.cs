using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Security;

namespace PaymentGateway.Web.Features.SuperAdmin.Users;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    public IndexModel(AppDbContext db, IPasswordHasher hasher) { _db = db; _hasher = hasher; }

    public IReadOnlyList<Row> Items { get; set; } = Array.Empty<Row>();
    public IReadOnlyList<PaymentGateway.Web.Domain.Entities.Company> Companies { get; set; } = Array.Empty<PaymentGateway.Web.Domain.Entities.Company>();
    [BindProperty] public Input New { get; set; } = new();

    public record Row(Guid Id, string Email, string FullName, string Role, string? Company, bool IsActive, DateTime? LastLoginAt);

    public class Input
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, StringLength(200)] public string FullName { get; set; } = "";
        [Required] public UserRole Role { get; set; } = UserRole.CompanyAdmin;
        public Guid? CompanyId { get; set; }
        [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = "";
    }

    public async Task OnGetAsync()
    {
        Items = await (
            from u in _db.Users.IgnoreQueryFilters()
            join c in _db.Companies.IgnoreQueryFilters() on u.CompanyId equals c.Id into gj
            from c in gj.DefaultIfEmpty()
            where u.DeletedAt == null
            orderby u.Email
            select new Row(u.Id, u.Email, u.FullName, u.Role.ToString(), c == null ? null : c.Name, u.IsActive, u.LastLoginAt)
        ).ToListAsync();

        Companies = await _db.Companies.IgnoreQueryFilters()
            .Where(c => c.DeletedAt == null).OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ModelState.IsValid) { await OnGetAsync(); return Page(); }
        if (New.Role == UserRole.SuperAdmin) New.CompanyId = null;
        else if (New.CompanyId == null)
        {
            ModelState.AddModelError("New.CompanyId", "Company required for non-super-admin role.");
            await OnGetAsync(); return Page();
        }

        if (_db.Users.IgnoreQueryFilters().Any(u => u.Email == New.Email))
        {
            ModelState.AddModelError("New.Email", "Email already in use.");
            await OnGetAsync(); return Page();
        }

        _db.Users.Add(new AppUser
        {
            Email = New.Email.Trim(),
            FullName = New.FullName.Trim(),
            Role = New.Role,
            CompanyId = New.CompanyId,
            PasswordHash = _hasher.Hash(New.Password),
            IsActive = true,
            MustChangePassword = true,
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "User added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid userId)
    {
        var u = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == userId);
        if (u != null) { u.IsActive = !u.IsActive; await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
