using Core.Models.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    public class EmployeeConfiguration : UserConfiguration<Employee>
    {
        public override void Configure(EntityTypeBuilder<Employee> builder)
        {
            base.Configure(builder);

            builder.Property(e => e.FirstName)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(e => e.LastName)
                 .IsRequired()
                 .HasMaxLength(255);
        }
    }
}
