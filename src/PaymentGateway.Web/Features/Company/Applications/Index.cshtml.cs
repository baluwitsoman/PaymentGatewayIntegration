using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Infrastructure.Persistence;

namespace PaymentGateway.Web.Features.Company.Applications;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public IReadOnlyList<CompanyApplication> Items { get; set; } = Array.Empty<CompanyApplication>();

    public async Task OnGetAsync()
    {
        Items = await _db.Applications.Include(a => a.ApiKeys)
            .OrderBy(a => a.Name).ToListAsync();
    }
}
