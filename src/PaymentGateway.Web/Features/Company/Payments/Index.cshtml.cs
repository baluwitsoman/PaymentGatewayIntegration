using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;

namespace PaymentGateway.Web.Features.Company.Payments;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public IReadOnlyList<Row> Items { get; set; } = Array.Empty<Row>();
    public string? Query { get; set; }
    public PaymentOrderStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int PageNumber { get; set; } = 1;
    public int Total { get; set; }
    public int PageSize { get; set; } = 25;

    public record Row(Guid Id, string Reference, string CustomerName, string CustomerCode,
        string? ExternalReference, long AmountMinor, string Currency,
        string Status, DateTime CreatedAt, DateTime? PaidAt, string AppName);

    public async Task OnGetAsync(string? q, PaymentOrderStatus? status, DateTime? from, DateTime? to, int page = 1)
    {
        Query = q; Status = status; From = from; To = to;
        PageNumber = Math.Max(1, page);

        var query = (
            from o in _db.PaymentOrders
            join c in _db.Customers on o.CustomerId equals c.Id
            join a in _db.Applications on o.CompanyApplicationId equals a.Id
            select new { o, c, a });

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.o.OrderReference.Contains(q) || x.c.FullName.Contains(q) || x.c.CustomerCode.Contains(q) ||
                                     (x.o.ExternalReference != null && x.o.ExternalReference.Contains(q)));
        if (status.HasValue) query = query.Where(x => x.o.Status == status.Value);
        if (from.HasValue) query = query.Where(x => x.o.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.o.CreatedAt < to.Value.AddDays(1));

        Total = await query.CountAsync();

        Items = await query.OrderByDescending(x => x.o.CreatedAt)
            .Skip((PageNumber - 1) * PageSize).Take(PageSize)
            .Select(x => new Row(
                x.o.Id, x.o.OrderReference, x.c.FullName, x.c.CustomerCode,
                x.o.ExternalReference, x.o.AmountMinor, x.o.Currency,
                x.o.Status.ToString(), x.o.CreatedAt, x.o.PaidAt, x.a.Name))
            .ToListAsync();
    }
}
