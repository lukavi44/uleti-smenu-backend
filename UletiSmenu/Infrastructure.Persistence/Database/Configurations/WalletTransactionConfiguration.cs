using Core.Models.Entities;
using Core.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
    {
        public void Configure(EntityTypeBuilder<WalletTransaction> builder)
        {
            builder.ToTable("WalletTransactions");
            builder.HasKey(transaction => transaction.Id);

            builder.Property(transaction => transaction.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(transaction => transaction.BalanceAfter)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(transaction => transaction.Type)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32);

            builder.Property(transaction => transaction.Description)
                .HasMaxLength(500);

            builder.Property(transaction => transaction.ExternalReference)
                .HasMaxLength(256);

            builder.Property(transaction => transaction.RelatedEntityType)
                .HasMaxLength(64);

            builder.HasIndex(transaction => transaction.EmployerId);
            builder.HasIndex(transaction => transaction.ExternalReference)
                .IsUnique()
                .HasFilter("[ExternalReference] IS NOT NULL");
        }
    }
}
