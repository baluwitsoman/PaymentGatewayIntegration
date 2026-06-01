using System.Text;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Security;

namespace PaymentGateway.Web.Features.PublicApi;

public record AuthenticatedApp(CompanyApplication Application, ApplicationApiKey Key);

public static class ApiKeyAuth
{
    public const string HeaderName = "X-Api-Key";

    public static async Task<AuthenticatedApp?> ResolveAsync(
        HttpContext ctx,
        AppDbContext db,
        IApiKeyGenerator generator,
        CancellationToken ct)
    {
        if (!ctx.Request.Headers.TryGetValue(HeaderName, out var headerVals))
            return null;
        var presented = headerVals.ToString();
        if (string.IsNullOrWhiteSpace(presented)) return null;

        var underscore = presented.IndexOf('_', presented.IndexOf('_') + 1);
        if (underscore < 0) return null;
        var prefix = presented[..(underscore + 1)];

        var candidates = await db.ApplicationApiKeys
            .IgnoreQueryFilters()
            .Include(k => k.Application)
            .Where(k => k.KeyPrefix == prefix && k.IsActive && k.RevokedAt == null)
            .ToListAsync(ct);

        var presentedHash = generator.HashKey(presented);
        foreach (var k in candidates)
        {
            if (FixedTimeEquals(k.KeyHash, presentedHash))
            {
                if (k.ExpiresAt.HasValue && k.ExpiresAt < DateTime.UtcNow) return null;
                if (!k.Application.IsActive || k.Application.DeletedAt != null) return null;
                k.LastUsedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                return new AuthenticatedApp(k.Application, k);
            }
        }
        return null;
    }

    private static bool FixedTimeEquals(byte[] a, byte[] b)
        => System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b);
}
