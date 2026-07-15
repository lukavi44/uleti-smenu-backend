namespace Core.DTOs
{
    public class ChatConversationListItemDTO
    {
        public Guid ConversationId { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid JobPostId { get; set; }
        public string JobPostTitle { get; set; } = string.Empty;
        public string? RestaurantLocationName { get; set; }
        public string? RestaurantLocationCity { get; set; }
        public string OtherPartyName { get; set; } = string.Empty;
        public Guid OtherPartyId { get; set; }
        public string? OtherPartyProfilePhoto { get; set; }
        public string? OtherPartyPublicSlug { get; set; }
        public string? LastMessagePreview { get; set; } = string.Empty;
        public DateTime? LastMessageAtUtc { get; set; }
        public int UnreadCount { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsReadOnly { get; set; }
        public bool CanSendMessages { get; set; }
    }
}
