using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Web.Domain.Entities;

namespace PaymentGateway.Web.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> b)
    {
        b.ToTable("company");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.CompCode).HasColumnName("comp_code").HasColumnType("citext").IsRequired();
        b.HasIndex(x => x.CompCode).IsUnique();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        b.Property(x => x.ContactEmail).HasColumnName("contact_email").HasColumnType("citext");
        b.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(30);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<short>();
        b.Property(x => x.DefaultCurrency).HasColumnName("default_currency").HasMaxLength(3).IsRequired();
        b.Property(x => x.Notes).HasColumnName("notes");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        // CompanyProviderConfig has many-to-one with Company (FK on the config side);
        // configured in CompanyProviderConfigConfiguration.
    }
}
