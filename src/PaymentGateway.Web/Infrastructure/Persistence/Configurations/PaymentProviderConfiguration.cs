using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class PaymentProviderConfiguration : IEntityTypeConfiguration<PaymentProvider>
{
    public void Configure(EntityTypeBuilder<PaymentProvider> b)
    {
        b.ToTable("payment_provider");
        b.HasKey(x => x.Code);
        b.Property(x => x.Code).HasColumnName("code").HasConversion<short>().ValueGeneratedNever();
        b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        b.Property(x => x.IsEnabled).HasColumnName("is_enabled");
        b.Property(x => x.DefaultBaseUrl).HasColumnName("default_base_url").HasMaxLength(200);
        b.Property(x => x.ExampleExtraConfigJson).HasColumnName("example_extra_config_json").HasColumnType("jsonb");
        b.Property(x => x.SortOrder).HasColumnName("sort_order");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
