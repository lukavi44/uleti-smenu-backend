using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.HasKey(message => message.Id);

            builder.Property(message => message.ConversationId).IsRequired();
            builder.Property(message => message.SenderId).IsRequired();
            builder.Property(message => message.Content).IsRequired().HasMaxLength(ChatMessage.MaxContentLength);
            builder.Property(message => message.SentAtUtc).IsRequired();

            builder.HasIndex(message => new { message.ConversationId, message.SentAtUtc });

            builder.ToTable("ChatMessages");
        }
    }
}
