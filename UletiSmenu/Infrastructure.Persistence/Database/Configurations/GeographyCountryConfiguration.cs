using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class GeographyCountryConfiguration : IEntityTypeConfiguration<GeographyCountry>
    {
        public void Configure(EntityTypeBuilder<GeographyCountry> builder)
        {
            builder.ToTable("GeographyCountries");
            builder.HasKey(country => country.Code);
            builder.Property(country => country.Code).HasMaxLength(2);
            builder.Property(country => country.Name).IsRequired().HasMaxLength(120);
            builder.Property(country => country.NativeName).IsRequired().HasMaxLength(120);
            builder.Property(country => country.IsActive).IsRequired().HasDefaultValue(true);
            builder.HasIndex(country => country.Name);
        }
    }
}
