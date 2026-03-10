using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    public class FavouriteConfiguration : IEntityTypeConfiguration<Favourite>
    {
        public void Configure(EntityTypeBuilder<Favourite> builder)
        {
            builder.HasKey(f => new { f.EmployeeId, f.EmployerId});

            builder.HasOne(f => f.Employee)
                   .WithMany()
                   .HasForeignKey(f => f.EmployeeId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Employer)
                   .WithMany()
                   .HasForeignKey(f => f.EmployerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable("Favourites");
        }
    }
}
