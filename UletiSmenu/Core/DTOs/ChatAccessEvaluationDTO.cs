using Core.Models.Entities;
using Core.Models.Enums;

namespace Core.DTOs
{
    public class ChatAccessEvaluationDTO
    {
        public Application Application { get; set; } = null!;
        public Conversation? Conversation { get; set; }
        public DateTime ShiftStartUtc { get; set; }
        public ConversationStatusEnum Status { get; set; }
        public bool CanRead { get; set; }
        public bool CanSend { get; set; }
        public bool IsArchived => Status == ConversationStatusEnum.Archived;
    }
}
