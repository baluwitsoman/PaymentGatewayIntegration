using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("app_user");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.Email).HasColumnName("email").HasColumnType("citext").IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
        b.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
        b.Property(x => x.Role).HasColumnName("role").HasConversion<short>();
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.FailedLoginCount).HasColumnName("failed_login_count");
        b.Property(x => x.LockedUntil).HasColumnName("locked_until");
        b.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        b.Property(x => x.MustChangePassword).HasColumnName("must_change_password");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        b.HasOne(x => x.Company).WithMany(c => c.Users)
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.SetNull);

        b.ToTable(t => t.HasCheckConstraint(
            "ck_app_user_role_company",
            "(role = 1 AND company_id IS NULL) OR (role IN (2,3) AND company_id IS NOT NULL)"));
    }
}
