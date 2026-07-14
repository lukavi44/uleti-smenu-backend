using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class RestaurantLocationConfiguration : IEntityTypeConfiguration<RestaurantLocation>
    {
        public void Configure(EntityTypeBuilder<RestaurantLocation> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(30);
            builder.Property(x => x.PIB).IsRequired().HasMaxLength(9);
            builder.Property(x => x.MB).IsRequired().HasMaxLength(8);
            builder.Property(x => x.StreetName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.StreetNumber).IsRequired().HasMaxLength(20);
            builder.Property(x => x.City).IsRequired().HasMaxLength(100);
            builder.Property(x => x.PostalCode).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Country).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Region).IsRequired().HasMaxLength(100);
            builder.Property(x => x.GeographyCountryCode).HasMaxLength(2).IsRequired(false);
            builder.Property(x => x.GeographyRegionCode).HasMaxLength(20).IsRequired(false);
            builder.Property(x => x.GeographyCityCode).HasMaxLength(20).IsRequired(false);

            builder.HasOne(x => x.Employer)
                .WithMany(e => e.Locations)
                .HasForeignKey(x => x.EmployerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.EmployerId);
            builder.HasIndex(x => new { x.EmployerId, x.Name });
            builder.HasIndex(x => x.GeographyCountryCode);
            builder.HasIndex(x => x.GeographyRegionCode);
            builder.HasIndex(x => x.GeographyCityCode);

            builder.HasOne(x => x.GeographyCountry)
                .WithMany()
                .HasForeignKey(x => x.GeographyCountryCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.GeographyRegion)
                .WithMany()
                .HasForeignKey(x => new { x.GeographyCountryCode, x.GeographyRegionCode })
                .HasPrincipalKey(region => new { region.CountryCode, region.Code })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.GeographyCity)
                .WithMany()
                .HasForeignKey(x => new { x.GeographyRegionCode, x.GeographyCityCode })
                .HasPrincipalKey(city => new { city.RegionCode, city.Code })
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable("RestaurantLocations");
        }
    }
}
