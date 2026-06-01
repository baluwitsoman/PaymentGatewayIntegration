using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("customer");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.CustomerCode).HasColumnName("customer_code").HasColumnType("citext").IsRequired();
        b.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
        b.Property(x => x.MobileNumber).HasColumnName("mobile_number").HasMaxLength(30);
        b.Property(x => x.Email).HasColumnName("email").HasColumnType("citext");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        b.HasOne(x => x.Company).WithMany(c => c.Customers)
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.CompanyId, x.CustomerCode }).IsUnique();
        b.HasIndex(x => new { x.CompanyId, x.MobileNumber });
    }
}
