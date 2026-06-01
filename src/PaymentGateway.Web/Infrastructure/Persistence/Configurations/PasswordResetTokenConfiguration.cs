using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> b)
    {
        b.ToTable("password_reset_token");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.AppUserId).HasColumnName("app_user_id");
        b.Property(x => x.TokenHash).HasColumnName("token_hash").IsRequired();
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        b.Property(x => x.UsedAt).HasColumnName("used_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.AppUserId).OnDelete(DeleteBehavior.Cascade);
    }
}
