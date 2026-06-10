using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Providers;
using PaymentGateway.Web.Infrastructure.Security;
using Serilog;

namespace PaymentGateway.Web.Infrastructure.Startup;

public static class DatabaseBootstrap
{
    public static async Task RunAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var registry = scope.ServiceProvider.GetRequiredService<IPaymentProviderRegistry>();

        // Apply pending migrations
        //await db.Database.MigrateAsync();

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

        await SeedProviderCatalogAndMappingsAsync(db, registry);
    }

    /// Idempotent. Seeds the provider catalog from the registered IPaymentProvider
    /// impls and backfills per-company mappings from existing provider configs so
    /// live behaviour is preserved (every already-configured provider stays
    /// available). Wrapped defensively: if the new tables are not present yet
    /// (manual SQL script not run), the app still starts and logs a clear warning.
    private static async Task SeedProviderCatalogAndMappingsAsync(AppDbContext db, IPaymentProviderRegistry registry)
    {
        try
        {
            // 1) Catalog: one row per provider that actually has an integration adapter.
            var existingCodes = await db.PaymentProviders.Select(p => p.Code).ToListAsync();
            var seen = existingCodes.ToHashSet();
            var added = 0;
            foreach (var prov in registry.All)
            {
                if (seen.Contains(prov.Code)) continue;
                var (url, example) = CatalogDefaults(prov.Code);
                db.PaymentProviders.Add(new PaymentProvider
                {
                    Code = prov.Code,
                    DisplayName = prov.DisplayName,
                    IsEnabled = true,
                    DefaultBaseUrl = url,
                    ExampleExtraConfigJson = example,
                    SortOrder = (short)prov.Code,
                });
                added++;
            }
            if (added > 0) await db.SaveChangesAsync();

            // 2) Backfill mappings from existing configs — preserves current UI/behaviour.
            var configPairs = await db.ProviderConfigs.IgnoreQueryFilters()
                .Select(c => new { c.CompanyId, c.ProviderCode })
                .Distinct().ToListAsync();
            var existingMaps = (await db.ProviderMappings.IgnoreQueryFilters()
                    .Select(m => new { m.CompanyId, m.ProviderCode }).ToListAsync())
                .Select(m => (m.CompanyId, m.ProviderCode)).ToHashSet();
            var mapsAdded = 0;
            foreach (var p in configPairs)
            {
                if (existingMaps.Contains((p.CompanyId, p.ProviderCode))) continue;
                db.ProviderMappings.Add(new CompanyProviderMapping
                {
                    CompanyId = p.CompanyId,
                    ProviderCode = p.ProviderCode,
                    IsEnabled = true,
                });
                mapsAdded++;
            }
            if (mapsAdded > 0) await db.SaveChangesAsync();

            if (added > 0 || mapsAdded > 0)
                Log.Information("Provider catalog seed: {Catalog} catalog row(s), {Maps} mapping row(s) added.", added, mapsAdded);
        }
        catch (Exception ex)
        {
            Log.Warning(ex,
                "Provider catalog/mapping seed skipped. If this is the first deploy of this feature, run the manual SQL script " +
                "(Infrastructure/Persistence/ManualScripts/2026-06-09_provider_catalog_and_mapping.sql) to create the new tables, then restart.");
        }
    }

    /// Default base URL + example extra-config seeded into the catalog the first
    /// time a provider appears. After seeding, SuperAdmin edits these in the DB.
    private static (string? Url, string? Example) CatalogDefaults(PaymentProviderCode code) => code switch
    {
        PaymentProviderCode.Paymob => ("https://accept.paymob.com", "{ \"iframeId\": \"\" }"),
        PaymentProviderCode.BankMuscat => ("https://hosted.bankmuscat.com", "{ \"merchantId\": \"\", \"terminalId\": \"\" }"),
        PaymentProviderCode.NBO => ("https://payments.nbo.om", "{ \"merchantId\": \"\" }"),
        _ => (null, null)
    };
}
