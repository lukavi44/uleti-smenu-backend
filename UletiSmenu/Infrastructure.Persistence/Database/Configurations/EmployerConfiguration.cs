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

            builder.Property(e => e.SubscriptionId)
                   .IsRequired(false);

            builder.Property(e => e.SubscriptionStart)
                   .IsRequired(false);

            builder.Property(e => e.SubscriptionStop)
                   .IsRequired(false);

            builder.OwnsOne(c => c.Address, address =>
            {
                // Configure Street
                address.OwnsOne(a => a.Street, street =>
                {
                    street.Property(s => s.Name)
                        .IsRequired()
                        .HasMaxLength(255);

                    street.Property(s => s.Number)
                        .IsRequired()
                        .HasMaxLength(20);
                });

                address.OwnsOne(a => a.City, city =>
                {
                    city.Property(c => c.Name)
                        .IsRequired()
                        .HasMaxLength(100);

                    city.OwnsOne(c => c.PostalCode, postalCode =>
                    {
                        postalCode.Property(p => p.Value)
                            .IsRequired()
                            .HasMaxLength(20);
                    });

                    city.OwnsOne(c => c.Country, country =>
                    {
                        country.Property(ct => ct.Name)
                            .IsRequired()
                            .HasMaxLength(100);
                    });

                    city.OwnsOne(c => c.Region, region =>
                    {
                        region.Property(r => r.Name)
                            .IsRequired()
                            .HasMaxLength(100);
                    });
                });
            });
        }
    }
}
