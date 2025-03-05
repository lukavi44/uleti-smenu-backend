using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Models.Entities;
using Core.Models.ValueObjects;

namespace Infrastructure.Persistence.Database.Configurations
{
    public abstract class UserConfiguration<T> : IEntityTypeConfiguration<T> where T : User
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            // ProfilePhoto - Optional, Max Length
            builder.Property(u => u.ProfilePhoto)
                .HasMaxLength(255);
            
            // Indexes for performance optimization
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.UserName).IsUnique();
            builder.HasIndex(u => u.PhoneNumber).IsUnique();

            // Table name (optional)
            builder.ToTable("AspNetUsers");
        }
    }
}
