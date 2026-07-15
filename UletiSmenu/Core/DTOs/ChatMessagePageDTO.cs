using System.Collections.Generic;

namespace Core.DTOs
{
    public class ChatMessagePageDTO
    {
        public List<ChatMessageDTO> Items { get; set; } = new();
        public bool HasMore { get; set; }
    }
}
