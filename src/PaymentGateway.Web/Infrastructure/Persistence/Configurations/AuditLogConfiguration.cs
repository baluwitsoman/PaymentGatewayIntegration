using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_log");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.AppUserId).HasColumnName("app_user_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(60).IsRequired();
        b.Property(x => x.EntityId).HasColumnName("entity_id");
        b.Property(x => x.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        b.Property(x => x.NewValues).HasColumnName("new_values").HasColumnType("jsonb");
        b.Property(x => x.IpAddress).HasColumnName("ip_address").HasColumnType("inet");
        b.Property(x => x.UserAgent).HasColumnName("user_agent");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        b.HasIndex(x => new { x.CompanyId, x.CreatedAt }).IsDescending(false, true);
        b.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
