using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Models.Entities; // Adjust namespace if needed

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            // Primary Key
            builder.HasKey(c => c.Id);

            // Name - Required, with max length
            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(255);


            // ✅ Map Address as an Owned Entity
            builder.OwnsOne(c => c.Address, address =>
            {
                // Configure Street
                address.OwnsOne(a => a.Street, street =>
                {
                    street.Property(s => s.Name)
                        .IsRequired()
                        .HasMaxLength(255);

                    street.Property(s => s.Number)
                        .IsRequired()
                        .HasMaxLength(20);
                });

                // Configure City
                address.OwnsOne(a => a.City, city =>
                {
                    city.Property(c => c.Name)
                        .IsRequired()
                        .HasMaxLength(100);

                    // PostalCode
                    city.OwnsOne(c => c.PostalCode, postalCode =>
                    {
                        postalCode.Property(p => p.Value)
                            .IsRequired()
                            .HasMaxLength(20);
                    });

                    // Country
                    city.OwnsOne(c => c.Country, country =>
                    {
                        country.Property(ct => ct.Name)
                            .IsRequired()
                            .HasMaxLength(100);
                    });

                    // Region
                    city.OwnsOne(c => c.Region, region =>
                    {
                        region.Property(r => r.Name)
                            .IsRequired()
                            .HasMaxLength(100);
                    });
                });
            });

            // Table Name (Optional)
            builder.ToTable("Companies");
        }
    }
}
