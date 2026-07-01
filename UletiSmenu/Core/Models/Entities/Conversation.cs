namespace Core.Models.Entities
{
    public class Conversation
    {
        public Guid Id { get; private set; }
        public Guid ApplicationId { get; private set; }
        public Guid EmployerId { get; private set; }
        public Guid EmployeeId { get; private set; }
        public Guid JobPostId { get; private set; }
        public Enums.ConversationStatusEnum Status { get; private set; } = Enums.ConversationStatusEnum.Active;
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? ArchivedAtUtc { get; private set; }
        public DateTime? LastMessageAtUtc { get; private set; }

        private Conversation() { }

        public static Conversation Create(
            Guid applicationId,
            Guid employerId,
            Guid employeeId,
            Guid jobPostId,
            DateTime createdAtUtc)
        {
            return new Conversation
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                EmployerId = employerId,
                EmployeeId = employeeId,
                JobPostId = jobPostId,
                Status = Enums.ConversationStatusEnum.Active,
                CreatedAtUtc = createdAtUtc
            };
        }

        public void Archive(DateTime archivedAtUtc)
        {
            if (Status == Enums.ConversationStatusEnum.Archived)
                return;

            Status = Enums.ConversationStatusEnum.Archived;
            ArchivedAtUtc = archivedAtUtc;
        }

        public void TouchLastMessageAt(DateTime sentAtUtc)
        {
            LastMessageAtUtc = sentAtUtc;
        }
    }
}
