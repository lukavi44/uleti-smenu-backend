using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);

            builder.Property(n => n.UserId).IsRequired();
            builder.Property(n => n.EmployerId).IsRequired();
            builder.Property(n => n.JobPostId).IsRequired();
            builder.Property(n => n.Type).IsRequired().HasMaxLength(100);
            builder.Property(n => n.Message).IsRequired().HasMaxLength(500);
            builder.Property(n => n.IsRead).IsRequired();
            builder.Property(n => n.IsDismissed).IsRequired().HasDefaultValue(false);
            builder.Property(n => n.CreatedAtUtc).IsRequired();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Employer>()
                .WithMany()
                .HasForeignKey(n => n.EmployerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<JobPost>()
                .WithMany()
                .HasForeignKey(n => n.JobPostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(n => new { n.UserId, n.CreatedAtUtc });
            builder.HasIndex(n => new { n.UserId, n.IsRead });
            builder.HasIndex(n => new { n.UserId, n.JobPostId, n.Type }).IsUnique();

            builder.ToTable("Notifications");
        }
    }
}
