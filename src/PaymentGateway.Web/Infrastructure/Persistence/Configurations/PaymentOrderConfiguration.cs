using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class PaymentOrderConfiguration : IEntityTypeConfiguration<PaymentOrder>
{
    public void Configure(EntityTypeBuilder<PaymentOrder> b)
    {
        b.ToTable("payment_order");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.OrderReference).HasColumnName("order_reference").HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.OrderReference).IsUnique();
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.CompanyApplicationId).HasColumnName("company_application_id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.ExternalReference).HasColumnName("external_reference").HasMaxLength(100);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        b.Property(x => x.AmountMinor).HasColumnName("amount_minor");
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<short>();
        b.Property(x => x.SelectedProviderCode).HasColumnName("selected_provider_code").HasConversion<short?>();
        b.Property(x => x.SelectedMethodId).HasColumnName("selected_method_id");
        b.Property(x => x.ProviderOrderId).HasColumnName("provider_order_id").HasMaxLength(100);
        b.Property(x => x.PaymentToken).HasColumnName("payment_token");
        b.Property(x => x.PaymentUrl).HasColumnName("payment_url").HasMaxLength(1000);
        b.Property(x => x.SuccessReturnUrl).HasColumnName("success_return_url").HasMaxLength(500).IsRequired();
        b.Property(x => x.FailureReturnUrl).HasColumnName("failure_return_url").HasMaxLength(500).IsRequired();
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        b.Property(x => x.PaidAt).HasColumnName("paid_at");
        b.Property(x => x.ClosedAt).HasColumnName("closed_at");

        b.Property(x => x.Xmin).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.CompanyApplicationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SelectedMethod).WithMany().HasForeignKey(x => x.SelectedMethodId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => new { x.CompanyId, x.CreatedAt }).IsDescending(false, true);
        b.HasIndex(x => new { x.CompanyId, x.Status });
        b.HasIndex(x => x.ProviderOrderId);
        b.HasIndex(x => new { x.CompanyId, x.ExternalReference });
        b.HasIndex(x => new { x.CompanyApplicationId, x.CreatedAt }).IsDescending(false, true);
    }
}
