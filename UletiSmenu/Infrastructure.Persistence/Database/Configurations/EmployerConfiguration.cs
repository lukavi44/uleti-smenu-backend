using Core.Models.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    public class EmployerConfiguration : UserConfiguration<Employer>
    {
        public override void Configure(EntityTypeBuilder<Employer> builder)
        {
            base.Configure(builder);

            builder.Property(e => e.Name)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(e => e.PIB)
                    .HasConversion(
                    pib => pib.Value,
                    pib => PIB.Create(pib).Value)
                   .IsRequired()
                   .HasMaxLength(9);

            builder.Property(e => e.MB)
                   .HasConversion(
                    mb => mb.Value,
                    mb => MB.Create(mb).Value)
                   .IsRequired()
                   .HasMaxLength(8);

            builder.Property(e => e.CompanyId)
                   .IsRequired();

            builder.Property(e => e.SubscriptionId)
                   .IsRequired(false);

            builder.Property(e => e.SubscriptionStart)
                   .IsRequired(false);

            builder.Property(e => e.SubscriptionStop)
                   .IsRequired(false);
        }
    }
}
