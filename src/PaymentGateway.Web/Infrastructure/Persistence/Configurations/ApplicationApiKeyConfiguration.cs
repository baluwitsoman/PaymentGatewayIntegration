using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class ApplicationApiKeyConfiguration : IEntityTypeConfiguration<ApplicationApiKey>
{
    public void Configure(EntityTypeBuilder<ApplicationApiKey> b)
    {
        b.ToTable("application_api_key");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.CompanyApplicationId).HasColumnName("company_application_id");
        b.Property(x => x.KeyPrefix).HasColumnName("key_prefix").HasMaxLength(12).IsRequired();
        b.Property(x => x.KeyHash).HasColumnName("key_hash").IsRequired();
        b.Property(x => x.Label).HasColumnName("label").HasMaxLength(100);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        b.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        b.Property(x => x.LastUsedAt).HasColumnName("last_used_at");

        b.HasOne(x => x.Application).WithMany(a => a.ApiKeys)
            .HasForeignKey(x => x.CompanyApplicationId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.KeyPrefix);
    }
}
