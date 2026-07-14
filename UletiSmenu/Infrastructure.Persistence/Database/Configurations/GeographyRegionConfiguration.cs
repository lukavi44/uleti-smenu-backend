using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class GeographyRegionConfiguration : IEntityTypeConfiguration<GeographyRegion>
    {
        public void Configure(EntityTypeBuilder<GeographyRegion> builder)
        {
            builder.ToTable("GeographyRegions");
            builder.HasKey(region => region.Code);
            builder.Property(region => region.Code).HasMaxLength(20);
            builder.Property(region => region.CountryCode).IsRequired().HasMaxLength(2);
            builder.Property(region => region.Name).IsRequired().HasMaxLength(160);
            builder.Property(region => region.NativeName).IsRequired().HasMaxLength(160);
            builder.Property(region => region.IsActive).IsRequired().HasDefaultValue(true);
            builder.HasAlternateKey(region => new { region.CountryCode, region.Code });

            builder.HasOne(region => region.Country)
                .WithMany(country => country.Regions)
                .HasForeignKey(region => region.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(region => new { region.CountryCode, region.Name });
        }
    }
}
