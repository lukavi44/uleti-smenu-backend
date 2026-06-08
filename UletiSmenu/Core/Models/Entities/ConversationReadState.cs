namespace Core.Models.Entities
{
    public class ConversationReadState
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid ConversationId { get; private set; }
        public DateTime LastReadAtUtc { get; private set; }

        private ConversationReadState() { }

        public static ConversationReadState Create(Guid userId, Guid conversationId, DateTime lastReadAtUtc)
        {
            return new ConversationReadState
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ConversationId = conversationId,
                LastReadAtUtc = lastReadAtUtc
            };
        }

        public void MarkRead(DateTime readAtUtc)
        {
            LastReadAtUtc = readAtUtc;
        }
    }
}
