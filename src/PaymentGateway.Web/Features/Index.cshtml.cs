using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PaymentGateway.Web.Infrastructure.Multitenancy;

namespace PaymentGateway.Web.Features;

public class IndexModel : PageModel
{
    private readonly ICurrentUser _user;
    public IndexModel(ICurrentUser user) => _user = user;

    public IActionResult OnGet()
    {
        if (!_user.IsAuthenticated) return RedirectToPage("/Auth/Login");
        if (_user.IsSuperAdmin) return RedirectToPage("/SuperAdmin/Companies/Index");
        return RedirectToPage("/Company/Dashboard/Index");
    }
}
