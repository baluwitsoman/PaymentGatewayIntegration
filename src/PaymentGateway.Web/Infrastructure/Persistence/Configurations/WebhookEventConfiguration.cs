using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> b)
    {
        b.ToTable("webhook_event");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.Source).HasColumnName("source").HasConversion<short>();
        b.Property(x => x.ProviderCode).HasColumnName("provider_code").HasConversion<short?>();
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.PaymentOrderId).HasColumnName("payment_order_id");
        b.Property(x => x.ProviderTransactionId).HasColumnName("provider_transaction_id").HasMaxLength(100);
        b.Property(x => x.Headers).HasColumnName("headers").HasColumnType("jsonb");
        b.Property(x => x.Body).HasColumnName("body").HasColumnType("jsonb");
        b.Property(x => x.HmacReceived).HasColumnName("hmac_received");
        b.Property(x => x.HmacComputed).HasColumnName("hmac_computed");
        b.Property(x => x.HmacValid).HasColumnName("hmac_valid");
        b.Property(x => x.ProcessingStatus).HasColumnName("processing_status").HasConversion<short>();
        b.Property(x => x.ProcessingError).HasColumnName("processing_error");
        b.Property(x => x.ReceivedAt).HasColumnName("received_at");
        b.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        b.Property(x => x.RemoteIp).HasColumnName("remote_ip").HasColumnType("inet");

        b.HasIndex(x => new { x.ProviderCode, x.ProviderTransactionId });
        b.HasIndex(x => x.ReceivedAt).IsDescending();
        b.HasIndex(x => new { x.ProcessingStatus, x.ReceivedAt });
    }
}
