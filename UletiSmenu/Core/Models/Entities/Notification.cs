namespace Core.Models.Entities
{
    public class Notification
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid EmployerId { get; private set; }
        public Guid JobPostId { get; private set; }
        public string Type { get; private set; } = string.Empty;
        public string Message { get; private set; } = string.Empty;
        public bool IsRead { get; private set; }
        public bool IsDismissed { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        private Notification() { }

        private Notification(
            Guid id,
            Guid userId,
            Guid employerId,
            Guid jobPostId,
            string type,
            string message,
            DateTime createdAtUtc)
        {
            Id = id;
            UserId = userId;
            EmployerId = employerId;
            JobPostId = jobPostId;
            Type = type;
            Message = message;
            CreatedAtUtc = createdAtUtc;
            IsRead = false;
        }

        public static Notification Create(
            Guid userId,
            Guid employerId,
            Guid jobPostId,
            string type,
            string message)
        {
            return new Notification(
                Guid.NewGuid(),
                userId,
                employerId,
                jobPostId,
                type,
                message,
                DateTime.UtcNow);
        }

        public void MarkAsRead()
        {
            IsRead = true;
        }

        public void Dismiss()
        {
            IsDismissed = true;
            IsRead = true;
        }
    }
}
