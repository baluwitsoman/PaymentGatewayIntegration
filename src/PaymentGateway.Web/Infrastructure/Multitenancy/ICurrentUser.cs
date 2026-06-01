using PaymentGateway.Web.Domain.Enums;

namespace PaymentGateway.Web.Infrastructure.Multitenancy;

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? CompanyId { get; }
    UserRole? Role { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsSuperAdmin { get; }
}
