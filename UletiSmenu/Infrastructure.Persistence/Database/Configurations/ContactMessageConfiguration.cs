using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
    {
        public void Configure(EntityTypeBuilder<ContactMessage> builder)
        {
            builder.HasKey(message => message.Id);

            builder.Property(message => message.Name).IsRequired().HasMaxLength(120);
            builder.Property(message => message.Email).IsRequired().HasMaxLength(256);
            builder.Property(message => message.Subject).IsRequired().HasMaxLength(160);
            builder.Property(message => message.Message).IsRequired().HasMaxLength(4000);
            builder.Property(message => message.CreatedAtUtc).IsRequired();
            builder.Property(message => message.Status).IsRequired().HasConversion<string>().HasMaxLength(32);
            builder.Property(message => message.EmailSent).IsRequired();
            builder.Property(message => message.AdminNotes).HasMaxLength(2000);

            builder.HasIndex(message => message.CreatedAtUtc);
            builder.HasIndex(message => message.Status);
            builder.HasIndex(message => message.Email);

            builder.ToTable("ContactMessages");
        }
    }
}
