using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Models.Entities;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class JobPostConfiguration : IEntityTypeConfiguration<JobPost>
    {
        public void Configure(EntityTypeBuilder<JobPost> builder)
        {
            builder.HasKey(j => j.Id);

            builder.Property(j => j.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(j => j.Description)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(j => j.Position)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(j => j.Status)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(j => j.Salary)
                .IsRequired();

            builder.Property(j => j.StartingDate)
                .IsRequired();

            builder.Property(j => j.EmployerId)
                .IsRequired();

            builder.HasOne(j => j.Employer)
                .WithMany(e => e.Posts)     
                .HasForeignKey(j => j.EmployerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(j => j.Position);
            builder.HasIndex(j => j.Salary);
            builder.HasIndex(j => j.StartingDate);

            builder.ToTable("JobPosts");
        }
    }
}
