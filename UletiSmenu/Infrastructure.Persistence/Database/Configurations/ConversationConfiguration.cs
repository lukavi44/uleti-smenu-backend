using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.HasKey(conversation => conversation.Id);

            builder.Property(conversation => conversation.ApplicationId).IsRequired();
            builder.Property(conversation => conversation.EmployerId).IsRequired();
            builder.Property(conversation => conversation.EmployeeId).IsRequired();
            builder.Property(conversation => conversation.JobPostId).IsRequired();
            builder.Property(conversation => conversation.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            builder.Property(conversation => conversation.CreatedAtUtc).IsRequired();
            builder.Property(conversation => conversation.ArchivedAtUtc);
            builder.Property(conversation => conversation.LastMessageAtUtc);

            builder.HasIndex(conversation => conversation.ApplicationId).IsUnique();
            builder.HasIndex(conversation => conversation.EmployerId);
            builder.HasIndex(conversation => conversation.EmployeeId);

            builder.ToTable("Conversations");
        }
    }
}
