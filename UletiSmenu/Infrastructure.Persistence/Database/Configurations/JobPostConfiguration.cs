using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Models.Entities;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class JobPostConfiguration : IEntityTypeConfiguration<JobPost>
    {
        public void Configure(EntityTypeBuilder<JobPost> builder)
        {
            // Primary Key
            builder.HasKey(j => j.Id);

            // Title - Required, Max Length
            builder.Property(j => j.Title)
                .IsRequired()
                .HasMaxLength(200);

            // Description - Required, Max Length
            builder.Property(j => j.Description)
                .IsRequired()
                .HasMaxLength(1000);

            // Position - Required, Max Length
            builder.Property(j => j.Position)
                .IsRequired()
                .HasMaxLength(150);

            // Status - Enum stored as string
            builder.Property(j => j.Status)
                .HasConversion<string>()
                .IsRequired();

            // Salary - Required
            builder.Property(j => j.Salary)
                .IsRequired();

            // Starting Date - Required
            builder.Property(j => j.StartingDate)
                .IsRequired();

            // CompanyId - Required Foreign Key
            builder.Property(j => j.CompanyId)
                .IsRequired();

            builder.HasOne<Company>()
                .WithMany() // No navigation properties
                .HasForeignKey(j => j.CompanyId)
                .OnDelete(DeleteBehavior.Cascade); // If Company is deleted, JobPosts are deleted

            builder.HasIndex(j => j.Position);
            builder.HasIndex(j => j.Salary);
            builder.HasIndex(j => j.StartingDate);

            // Table name (optional)
            builder.ToTable("JobPosts");
        }
    }
}
