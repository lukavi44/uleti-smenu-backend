using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEvent>
    {
        public void Configure(EntityTypeBuilder<PaymentEvent> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.ProviderEventId)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasIndex(e => e.ProviderEventId)
                .IsUnique();

            builder.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(e => e.Payload)
                .IsRequired();

            builder.Property(e => e.ProcessedAtUtc)
                .IsRequired();

            builder.ToTable("PaymentEvents");
        }
    }
}
