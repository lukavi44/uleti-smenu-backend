using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class ConversationReadStateConfiguration : IEntityTypeConfiguration<ConversationReadState>
    {
        public void Configure(EntityTypeBuilder<ConversationReadState> builder)
        {
            builder.HasKey(state => state.Id);

            builder.Property(state => state.UserId).IsRequired();
            builder.Property(state => state.ConversationId).IsRequired();
            builder.Property(state => state.LastReadAtUtc).IsRequired();

            builder.HasIndex(state => new { state.UserId, state.ConversationId }).IsUnique();

            builder.ToTable("ConversationReadStates");
        }
    }
}
