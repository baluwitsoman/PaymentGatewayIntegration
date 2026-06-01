using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Security;

namespace PaymentGateway.Web.Infrastructure.Startup;

public static class DatabaseBootstrap
{
    public static async Task RunAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Apply pending migrations
        await db.Database.MigrateAsync();

        // Seed super admin if missing
        var seedEmail = config["Seed:SuperAdminEmail"] ?? "admin@payment-gateway.local";
        var seedPassword = config["Seed:SuperAdminPassword"] ?? "ChangeMe!2026";

        var existing = await db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Email == seedEmail);
        if (!existing)
        {
            db.Users.Add(new AppUser
            {
                Email = seedEmail,
                FullName = "Super Admin",
                PasswordHash = hasher.Hash(seedPassword),
                Role = UserRole.SuperAdmin,
                IsActive = true,
                MustChangePassword = true,
            });
            await db.SaveChangesAsync();
        }
    }
}
