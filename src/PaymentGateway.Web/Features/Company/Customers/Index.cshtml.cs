using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;

namespace PaymentGateway.Web.Features.Company.Customers;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public IReadOnlyList<Row> Items { get; set; } = Array.Empty<Row>();
    public string? Query { get; set; }
    public int PageNumber { get; set; } = 1;
    public int Total { get; set; }
    public int PageSize { get; set; } = 25;

    public record Row(Guid Id, string CustomerCode, string FullName, string? MobileNumber, string? Email,
        int OrderCount, long TotalPaidMinor, DateTime? LastPaymentAt);

    public async Task OnGetAsync(string? q, int page = 1)
    {
        Query = q;
        PageNumber = Math.Max(1, page);

        var customers = _db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            customers = customers.Where(c => c.CustomerCode.Contains(q) || c.FullName.Contains(q) || (c.MobileNumber != null && c.MobileNumber.Contains(q)));

        Total = await customers.CountAsync();

        Items = await (
            from c in customers.OrderBy(c => c.FullName).Skip((PageNumber - 1) * PageSize).Take(PageSize)
            select new Row(
                c.Id, c.CustomerCode, c.FullName, c.MobileNumber, c.Email,
                _db.PaymentOrders.Count(o => o.CustomerId == c.Id),
                _db.PaymentTransactions.Where(t => t.Order.CustomerId == c.Id && t.IsSuccess && !t.IsRefund && !t.IsVoid)
                    .Sum(t => (long?)t.AmountMinor) ?? 0L,
                _db.PaymentOrders.Where(o => o.CustomerId == c.Id && o.PaidAt != null)
                    .Max(o => (DateTime?)o.PaidAt))
        ).ToListAsync();
    }
}
