using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class ChatMessage
    {
        public const int MaxContentLength = 2000;

        public Guid Id { get; private set; }
        public Guid ConversationId { get; private set; }
        public Guid SenderId { get; private set; }
        public string Content { get; private set; } = string.Empty;
        public DateTime SentAtUtc { get; private set; }

        private ChatMessage() { }

        public static Result<ChatMessage> Create(
            Guid conversationId,
            Guid senderId,
            string content,
            DateTime sentAtUtc)
        {
            if (conversationId == Guid.Empty)
                return Result.Failure<ChatMessage>("Conversation ID cannot be empty.");

            if (senderId == Guid.Empty)
                return Result.Failure<ChatMessage>("Sender ID cannot be empty.");

            var normalizedContent = content?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedContent))
                return Result.Failure<ChatMessage>("Message cannot be empty.");

            if (normalizedContent.Length > MaxContentLength)
                return Result.Failure<ChatMessage>($"Message cannot exceed {MaxContentLength} characters.");

            return Result.Success(new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = senderId,
                Content = normalizedContent,
                SentAtUtc = sentAtUtc
            });
        }
    }
}
