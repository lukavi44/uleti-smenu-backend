namespace Core.DTOs
{
    public class ChatMessageDTO
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAtUtc { get; set; }
    }
}
