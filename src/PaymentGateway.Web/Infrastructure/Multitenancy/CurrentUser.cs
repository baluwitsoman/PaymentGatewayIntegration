using System.Security.Claims;
using PaymentGateway.Web.Domain.Enums;

namespace PaymentGateway.Web.Infrastructure.Multitenancy;

public class CurrentUser : ICurrentUser
{
    public const string ClaimCompanyId = "company_id";
    public const string ClaimRole = "app_role";

    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var v = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(v, out var id) ? id : null;
        }
    }

    public Guid? CompanyId
    {
        get
        {
            var v = Principal?.FindFirstValue(ClaimCompanyId);
            return Guid.TryParse(v, out var id) ? id : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var v = Principal?.FindFirstValue(ClaimRole);
            return short.TryParse(v, out var s) ? (UserRole)s : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;
}
