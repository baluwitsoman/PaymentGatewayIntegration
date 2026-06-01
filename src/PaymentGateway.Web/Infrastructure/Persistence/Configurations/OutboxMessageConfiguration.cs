using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("outbox_message");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.CompanyApplicationId).HasColumnName("company_application_id");
        b.Property(x => x.PaymentOrderId).HasColumnName("payment_order_id");
        b.Property(x => x.TargetUrl).HasColumnName("target_url").HasMaxLength(500).IsRequired();
        b.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb");
        b.Property(x => x.Signature).HasColumnName("signature");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<short>();
        b.Property(x => x.AttemptCount).HasColumnName("attempt_count");
        b.Property(x => x.MaxAttempts).HasColumnName("max_attempts");
        b.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at");
        b.Property(x => x.LastAttemptedAt).HasColumnName("last_attempted_at");
        b.Property(x => x.LastResponseCode).HasColumnName("last_response_code");
        b.Property(x => x.LastError).HasColumnName("last_error");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.DeliveredAt).HasColumnName("delivered_at");

        b.HasIndex(x => new { x.Status, x.NextAttemptAt });
    }
}
