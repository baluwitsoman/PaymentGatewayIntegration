using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Multitenancy;
using PaymentGateway.Web.Infrastructure.Persistence;

namespace PaymentGateway.Web.Features.Company.Dashboard;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    public IndexModel(AppDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public string CompanyName { get; set; } = "";
    public long TotalReceivedMinor { get; set; }
    public long TodayReceivedMinor { get; set; }
    public int TotalPaidCount { get; set; }
    public int TotalFailedCount { get; set; }
    public int CustomersCount { get; set; }
    public IReadOnlyList<DailyPoint> Last30Days { get; set; } = Array.Empty<DailyPoint>();
    public IReadOnlyList<RecentRow> Recent { get; set; } = Array.Empty<RecentRow>();

    public record DailyPoint(DateOnly Date, long AmountMinor, int Count);
    public record RecentRow(string Reference, string CustomerName, long AmountMinor, string Currency, string Status, DateTime CreatedAt);

    public async Task<IActionResult> OnGetAsync()
    {
        if (_user.CompanyId is not Guid cid) return Forbid();
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == cid);
        if (company == null) return NotFound();
        CompanyName = company.Name;

        var today = DateTime.UtcNow.Date;
        var from = today.AddDays(-29);

        TotalReceivedMinor = await _db.PaymentTransactions
            .Where(t => t.IsSuccess && !t.IsRefund && !t.IsVoid)
            .SumAsync(t => (long?)t.AmountMinor) ?? 0L;

        TodayReceivedMinor = await _db.PaymentTransactions
            .Where(t => t.IsSuccess && !t.IsRefund && !t.IsVoid && t.ReceivedAt >= today)
            .SumAsync(t => (long?)t.AmountMinor) ?? 0L;

        TotalPaidCount = await _db.PaymentOrders.CountAsync(o => o.Status == PaymentOrderStatus.Paid);
        TotalFailedCount = await _db.PaymentOrders.CountAsync(o => o.Status == PaymentOrderStatus.Failed);
        CustomersCount = await _db.Customers.CountAsync();

        var daily = await _db.PaymentTransactions
            .Where(t => t.IsSuccess && !t.IsRefund && !t.IsVoid && t.ReceivedAt >= from)
            .GroupBy(t => t.ReceivedAt.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(x => x.AmountMinor), Count = g.Count() })
            .ToListAsync();

        var dict = daily.ToDictionary(d => DateOnly.FromDateTime(d.Date), d => (d.Amount, d.Count));
        var series = new List<DailyPoint>();
        for (var i = 0; i < 30; i++)
        {
            var date = DateOnly.FromDateTime(from.AddDays(i));
            var (amt, cnt) = dict.TryGetValue(date, out var v) ? v : (0L, 0);
            series.Add(new DailyPoint(date, amt, cnt));
        }
        Last30Days = series;

        Recent = await (
            from o in _db.PaymentOrders
            join c in _db.Customers on o.CustomerId equals c.Id
            orderby o.CreatedAt descending
            select new RecentRow(o.OrderReference, c.FullName, o.AmountMinor, o.Currency, o.Status.ToString(), o.CreatedAt)
        ).Take(10).ToListAsync();

        return Page();
    }
}
