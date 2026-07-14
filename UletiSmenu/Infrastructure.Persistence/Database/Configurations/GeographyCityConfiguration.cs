using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class GeographyCityConfiguration : IEntityTypeConfiguration<GeographyCity>
    {
        public void Configure(EntityTypeBuilder<GeographyCity> builder)
        {
            builder.ToTable("GeographyCities");
            builder.HasKey(city => city.Code);
            builder.Property(city => city.Code).HasMaxLength(20);
            builder.Property(city => city.RegionCode).IsRequired().HasMaxLength(20);
            builder.Property(city => city.Name).IsRequired().HasMaxLength(160);
            builder.Property(city => city.NativeName).IsRequired().HasMaxLength(160);
            builder.Property(city => city.IsActive).IsRequired().HasDefaultValue(true);
            builder.HasAlternateKey(city => new { city.RegionCode, city.Code });

            builder.HasOne(city => city.Region)
                .WithMany(region => region.Cities)
                .HasForeignKey(city => city.RegionCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(city => new { city.RegionCode, city.Name });
        }
    }
}
