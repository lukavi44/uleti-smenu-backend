using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    public abstract class UserConfiguration<T> : IEntityTypeConfiguration<T> where T : User
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            // ProfilePhoto - Optional, Max Length
            builder.Property(u => u.ProfilePhoto)
                .HasMaxLength(255);

            builder.Property(u => u.DeletedAtUtc);

            // Indexes for performance optimization
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.UserName).IsUnique();
            builder.HasIndex(u => u.PhoneNumber)
                .IsUnique()
                .HasFilter("[PhoneNumber] IS NOT NULL AND [PhoneNumber] <> ''");
            builder.HasIndex(u => u.DeletedAtUtc);

            // Table name (optional)
            builder.ToTable("AspNetUsers");
        }
    }
}
