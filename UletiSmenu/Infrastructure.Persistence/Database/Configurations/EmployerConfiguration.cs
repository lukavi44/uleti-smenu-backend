using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
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

            builder.Property(e => e.PublicSlug)
                   .IsRequired()
                   .HasMaxLength(160);

            builder.HasIndex(e => e.PublicSlug)
                   .IsUnique();

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

            builder.Property(e => e.BillingStatus)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(32);

            builder.Property(e => e.StripeCustomerId)
                   .HasMaxLength(128);

            builder.Property(e => e.StripeSubscriptionId)
                   .HasMaxLength(128);

            builder.Property(e => e.StripePriceId)
                   .HasMaxLength(128);

            builder.Property(e => e.CurrentPeriodEndUtc)
                   .IsRequired(false);

            builder.Property(e => e.TrialEndsAtUtc)
                   .IsRequired(false);

            builder.Property(e => e.GracePeriodEndsAtUtc)
                   .IsRequired(false);

            builder.Property(e => e.PostCredits)
                   .IsRequired()
                   .HasDefaultValue(0);

            builder.Property(e => e.WalletBalance)
                   .IsRequired()
                   .HasPrecision(18, 2)
                   .HasDefaultValue(0m);

            builder.Property(e => e.BillingProvider)
                   .IsRequired()
                   .HasMaxLength(32)
                   .HasDefaultValue("None");

            builder.Property(e => e.IsVerifiedEmployer)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(e => e.VerifiedAtUtc)
                   .IsRequired(false);

            builder.Property(e => e.VerifiedByUserId)
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
