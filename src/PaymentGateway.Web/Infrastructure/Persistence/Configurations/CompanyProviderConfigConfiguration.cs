using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class CompanyProviderConfigConfiguration : IEntityTypeConfiguration<CompanyProviderConfig>
{
    public void Configure(EntityTypeBuilder<CompanyProviderConfig> b)
    {
        b.ToTable("company_provider_config");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        b.Property(x => x.ProviderCode).HasColumnName("provider_code").HasConversion<short>();
        b.HasIndex(x => new { x.CompanyId, x.ProviderCode }).IsUnique();

        b.Property(x => x.Environment).HasColumnName("environment").HasConversion<short>();
        b.Property(x => x.ApiKeyEncrypted).HasColumnName("api_key_encrypted").IsRequired();
        b.Property(x => x.PublicKeyEncrypted).HasColumnName("public_key_encrypted");
        b.Property(x => x.SecretKeyEncrypted).HasColumnName("secret_key_encrypted");
        b.Property(x => x.HmacSecretEncrypted).HasColumnName("hmac_secret_encrypted").IsRequired();
        b.Property(x => x.ExtraConfigJson).HasColumnName("extra_config_json").HasColumnType("jsonb");
        b.Property(x => x.BaseUrl).HasColumnName("base_url").HasMaxLength(200);
        b.Property(x => x.DisplayLabel).HasColumnName("display_label").HasMaxLength(100);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.SortOrder).HasColumnName("sort_order");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        b.HasOne(x => x.Company).WithMany(c => c.Providers)
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}
