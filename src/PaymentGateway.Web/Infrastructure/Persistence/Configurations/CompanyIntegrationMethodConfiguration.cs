using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class CompanyIntegrationMethodConfiguration : IEntityTypeConfiguration<CompanyIntegrationMethod>
{
    public void Configure(EntityTypeBuilder<CompanyIntegrationMethod> b)
    {
        b.ToTable("company_integration_method");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.ProviderCode).HasColumnName("provider_code").HasConversion<short>();
        b.Property(x => x.MethodType).HasColumnName("method_type").HasConversion<short>();
        b.Property(x => x.ProviderIntegrationId).HasColumnName("provider_integration_id").HasMaxLength(100).IsRequired();
        b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        b.Property(x => x.SortOrder).HasColumnName("sort_order");
        b.Property(x => x.IsEnabled).HasColumnName("is_enabled");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        b.HasOne(x => x.Company).WithMany(c => c.IntegrationMethods)
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.CompanyId, x.ProviderCode, x.MethodType, x.ProviderIntegrationId }).IsUnique();
        b.HasIndex(x => new { x.CompanyId, x.ProviderCode });
    }
}
