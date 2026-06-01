using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> b)
    {
        b.ToTable("payment_transaction");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.PaymentOrderId).HasColumnName("payment_order_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.ProviderCode).HasColumnName("provider_code").HasConversion<short>();
        b.Property(x => x.ProviderTransactionId).HasColumnName("provider_transaction_id").HasMaxLength(100).IsRequired();
        b.HasIndex(x => new { x.ProviderCode, x.ProviderTransactionId }).IsUnique();

        b.Property(x => x.IsSuccess).HasColumnName("is_success");
        b.Property(x => x.IsPending).HasColumnName("is_pending");
        b.Property(x => x.IsRefund).HasColumnName("is_refund");
        b.Property(x => x.IsVoid).HasColumnName("is_void");
        b.Property(x => x.Is3DSecure).HasColumnName("is_3d_secure");
        b.Property(x => x.AmountMinor).HasColumnName("amount_minor");
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
        b.Property(x => x.ProviderIntegrationId).HasColumnName("provider_integration_id").HasMaxLength(100).IsRequired();
        b.Property(x => x.SourceType).HasColumnName("source_type").HasMaxLength(30);
        b.Property(x => x.SourceData).HasColumnName("source_data").HasColumnType("jsonb");
        b.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(50);
        b.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(500);
        b.Property(x => x.HmacValid).HasColumnName("hmac_valid");
        b.Property(x => x.ReceivedAt).HasColumnName("received_at");
        b.Property(x => x.ProcessedAt).HasColumnName("processed_at");

        b.HasOne(x => x.Order).WithMany(o => o.Transactions)
            .HasForeignKey(x => x.PaymentOrderId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.PaymentOrderId);
        b.HasIndex(x => new { x.CompanyId, x.ReceivedAt }).IsDescending(false, true);
        b.HasIndex(x => new { x.CompanyId, x.IsSuccess, x.ReceivedAt }).IsDescending(false, false, true);
    }
}
