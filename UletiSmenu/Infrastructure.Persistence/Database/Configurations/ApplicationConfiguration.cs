using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Models.Entities;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class ApplicationConfiguration : IEntityTypeConfiguration<Application>
    {
        public void Configure(EntityTypeBuilder<Application> builder)
        {
            // Primary Key
            builder.HasKey(a => a.Id);

            // UserId - Required Foreign Key
            builder.Property(a => a.UserId)
                .IsRequired();

            builder.HasOne<User>()
                .WithMany()  // No navigation properties
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade); // If User is deleted, Application is deleted

            // JobPostId - Required Foreign Key
            builder.Property(a => a.JobPostId)
                .IsRequired();

            builder.HasOne<JobPost>()
                .WithMany()  // No navigation properties
                .HasForeignKey(a => a.JobPostId)
                .OnDelete(DeleteBehavior.Cascade); // If JobPost is deleted, Application is deleted

            // Status - Enum stored as string
            builder.Property(a => a.Status)
                .HasConversion<string>()
                .IsRequired();

            // NumberOfApplicants - Required
            builder.Property(a => a.NumberOfApplicants)
                .IsRequired();

            // DateTime - Required, default to UTC
            builder.Property(a => a.DateTime)
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            // Table name (optional)
            builder.ToTable("Applications");
        }
    }
}
