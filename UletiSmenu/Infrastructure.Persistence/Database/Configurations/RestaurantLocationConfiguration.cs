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
            builder.Property(x => x.StreetName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.StreetNumber).IsRequired().HasMaxLength(20);
            builder.Property(x => x.City).IsRequired().HasMaxLength(100);
            builder.Property(x => x.PostalCode).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Country).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Region).IsRequired().HasMaxLength(100);

            builder.HasOne(x => x.Employer)
                .WithMany(e => e.Locations)
                .HasForeignKey(x => x.EmployerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.EmployerId);
            builder.HasIndex(x => new { x.EmployerId, x.Name });

            builder.ToTable("RestaurantLocations");
        }
    }
}
