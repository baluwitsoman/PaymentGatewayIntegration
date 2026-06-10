using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Infrastructure.Multitenancy;

namespace PaymentGateway.Web.Infrastructure.Persistence;

public class AppDbContext : DbContext, IDataProtectionKeyContext
{
    private readonly ICurrentUser? _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser? currentUser = null) : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<PaymentProvider> PaymentProviders => Set<PaymentProvider>();
    public DbSet<CompanyProviderMapping> ProviderMappings => Set<CompanyProviderMapping>();
    public DbSet<CompanyProviderConfig> ProviderConfigs => Set<CompanyProviderConfig>();
    public DbSet<CompanyIntegrationMethod> IntegrationMethods => Set<CompanyIntegrationMethod>();
    public DbSet<CompanyApplication> Applications => Set<CompanyApplication>();
    public DbSet<ApplicationApiKey> ApplicationApiKeys => Set<ApplicationApiKey>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("pgcrypto");
        b.HasPostgresExtension("citext");

        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Tenancy global query filter — applied when CurrentUser is a CompanyAdmin/Viewer.
        // SuperAdmin or background services bypass via IgnoreQueryFilters().
        b.Entity<CompanyApplication>().HasQueryFilter(e =>
            e.DeletedAt == null &&
            (_currentUser == null || _currentUser.IsSuperAdmin || _currentUser.CompanyId == e.CompanyId));
        b.Entity<CompanyIntegrationMethod>().HasQueryFilter(e =>
            _currentUser == null || _currentUser.IsSuperAdmin || _currentUser.CompanyId == e.CompanyId);
        b.Entity<Customer>().HasQueryFilter(e =>
            e.DeletedAt == null &&
            (_currentUser == null || _currentUser.IsSuperAdmin || _currentUser.CompanyId == e.CompanyId));
        b.Entity<PaymentOrder>().HasQueryFilter(e =>
            _currentUser == null || _currentUser.IsSuperAdmin || _currentUser.CompanyId == e.CompanyId);
        b.Entity<PaymentTransaction>().HasQueryFilter(e =>
            _currentUser == null || _currentUser.IsSuperAdmin || _currentUser.CompanyId == e.CompanyId);
        b.Entity<Company>().HasQueryFilter(e => e.DeletedAt == null);
        b.Entity<AppUser>().HasQueryFilter(e => e.DeletedAt == null);
    }

    public override int SaveChanges()
    {
        Stamp();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Stamp();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void Stamp()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Metadata.FindProperty("CreatedAt") != null)
                {
                    var current = (DateTime?)entry.Property("CreatedAt").CurrentValue;
                    if (current == default || current == DateTime.MinValue) entry.Property("CreatedAt").CurrentValue = now;
                }
                if (entry.Metadata.FindProperty("UpdatedAt") != null) entry.Property("UpdatedAt").CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified && entry.Metadata.FindProperty("UpdatedAt") != null)
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }
}
