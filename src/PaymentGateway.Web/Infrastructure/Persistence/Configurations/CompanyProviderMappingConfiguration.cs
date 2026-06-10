using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class CompanyProviderMappingConfiguration : IEntityTypeConfiguration<CompanyProviderMapping>
{
    public void Configure(EntityTypeBuilder<CompanyProviderMapping> b)
    {
        b.ToTable("company_provider_mapping");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        b.Property(x => x.ProviderCode).HasColumnName("provider_code").HasConversion<short>();
        b.HasIndex(x => new { x.CompanyId, x.ProviderCode }).IsUnique();

        b.Property(x => x.IsEnabled).HasColumnName("is_enabled");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        b.HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}
