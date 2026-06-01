using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Infrastructure.Persistence;

namespace PaymentGateway.Web.Features.SuperAdmin.Companies;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public IReadOnlyList<Row> Items { get; set; } = Array.Empty<Row>();
    public string? Query { get; set; }

    public record Row(Guid Id, string CompCode, string Name, string? Email, string Status,
        int Applications, int CustomersCount, long TotalPaidMinor);

    public async Task OnGetAsync(string? q)
    {
        Query = q;
        var query = _db.Companies.IgnoreQueryFilters()
            .Where(c => c.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(c => c.CompCode.Contains(q) || c.Name.Contains(q));

        Items = await query
            .OrderBy(c => c.Name)
            .Select(c => new Row(
                c.Id, c.CompCode, c.Name, c.ContactEmail, c.Status.ToString(),
                _db.Applications.IgnoreQueryFilters().Count(a => a.CompanyId == c.Id && a.DeletedAt == null),
                _db.Customers.IgnoreQueryFilters().Count(cu => cu.CompanyId == c.Id && cu.DeletedAt == null),
                _db.PaymentTransactions.IgnoreQueryFilters()
                    .Where(t => t.CompanyId == c.Id && t.IsSuccess && !t.IsRefund && !t.IsVoid)
                    .Sum(t => (long?)t.AmountMinor) ?? 0L))
            .ToListAsync();
    }
}
