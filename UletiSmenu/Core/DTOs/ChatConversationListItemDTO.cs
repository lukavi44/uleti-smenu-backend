namespace Core.DTOs
{
    public class ChatConversationListItemDTO
    {
        public Guid ConversationId { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid JobPostId { get; set; }
        public string JobPostTitle { get; set; } = string.Empty;
        public string OtherPartyName { get; set; } = string.Empty;
        public string? LastMessagePreview { get; set; }
        public DateTime? LastMessageAtUtc { get; set; }
        public int UnreadCount { get; set; }
    }
}
