using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class CompanyApplicationConfiguration : IEntityTypeConfiguration<CompanyApplication>
{
    public void Configure(EntityTypeBuilder<CompanyApplication> b)
    {
        b.ToTable("company_application");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.AppCode).HasColumnName("app_code").HasColumnType("citext").IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        b.Property(x => x.SuccessReturnUrl).HasColumnName("success_return_url").HasMaxLength(500).IsRequired();
        b.Property(x => x.FailureReturnUrl).HasColumnName("failure_return_url").HasMaxLength(500).IsRequired();
        b.Property(x => x.PendingReturnUrl).HasColumnName("pending_return_url").HasMaxLength(500);
        b.Property(x => x.WebhookUrl).HasColumnName("webhook_url").HasMaxLength(500);
        b.Property(x => x.WebhookSecretEncrypted).HasColumnName("webhook_secret_encrypted");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        b.HasOne(x => x.Company).WithMany(c => c.Applications)
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.CompanyId, x.AppCode }).IsUnique();
    }
}
