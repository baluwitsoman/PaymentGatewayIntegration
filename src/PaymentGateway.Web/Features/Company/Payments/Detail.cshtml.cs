using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Infrastructure.Persistence;

namespace PaymentGateway.Web.Features.Company.Payments;

public class DetailModel : PageModel
{
    private readonly AppDbContext _db;
    public DetailModel(AppDbContext db) => _db = db;

    public PaymentOrder Order { get; set; } = default!;
    public Customer Customer { get; set; } = default!;
    public CompanyApplication App { get; set; } = default!;
    public IReadOnlyList<PaymentTransaction> Transactions { get; set; } = Array.Empty<PaymentTransaction>();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Order = (await _db.PaymentOrders
            .Include(o => o.Customer)
            .Include(o => o.Application)
            .Include(o => o.Transactions)
            .FirstOrDefaultAsync(o => o.Id == id))!;
        if (Order == null) return NotFound();
        Customer = Order.Customer;
        App = Order.Application;
        Transactions = Order.Transactions.OrderByDescending(t => t.ReceivedAt).ToList();
        return Page();
    }
}
