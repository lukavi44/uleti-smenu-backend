using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class WorkExperienceConfiguration : IEntityTypeConfiguration<WorkExperience>
    {
        public void Configure(EntityTypeBuilder<WorkExperience> builder)
        {
            builder.HasKey(experience => experience.Id);

            builder.Property(experience => experience.EmployeeId).IsRequired();
            builder.Property(experience => experience.CompanyName).IsRequired().HasMaxLength(WorkExperience.MaxCompanyNameLength);
            builder.Property(experience => experience.Position).IsRequired().HasMaxLength(WorkExperience.MaxPositionLength);
            builder.Property(experience => experience.StartDate).IsRequired();
            builder.Property(experience => experience.Description).HasMaxLength(WorkExperience.MaxDescriptionLength);

            builder.HasIndex(experience => experience.EmployeeId);

            builder.ToTable("WorkExperiences");
        }
    }
}
