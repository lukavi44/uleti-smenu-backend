using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Models.Entities;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            // Primary Key
            builder.HasKey(s => s.Id);

            // Title - Required, Max Length
            builder.Property(s => s.Title)
                .IsRequired()
                .HasMaxLength(200);

            // Description - Required, Max Length
            builder.Property(s => s.Description)
                .IsRequired()
                .HasMaxLength(1000);

            // Cost - Required, Must Be Positive
            builder.Property(s => s.Cost)
                .IsRequired()
                .HasPrecision(10, 2); // Ensures proper decimal storage

            // DurationInDays - Required, Must Be Positive
            builder.Property(s => s.DurationInDays)
                .IsRequired();

            // NumberOfPosts - Required, Must Be Non-Negative
            builder.Property(s => s.NumberOfPosts)
                .IsRequired();

            // Indexes for optimization (if necessary)
            builder.HasIndex(s => s.Cost); // If filtering by cost
            builder.HasIndex(s => s.DurationInDays); // If filtering by duration

            // Table name (optional)
            builder.ToTable("Subscriptions");
        }
    }
}
