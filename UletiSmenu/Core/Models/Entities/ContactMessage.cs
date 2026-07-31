using Core.Models.Enums;
using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class ContactMessage
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string Subject { get; private set; } = string.Empty;
        public string Message { get; private set; } = string.Empty;
        public DateTime CreatedAtUtc { get; private set; }
        public ContactMessageStatus Status { get; private set; }
        public bool EmailSent { get; private set; }
        public DateTime? ResolvedAtUtc { get; private set; }
        public Guid? ResolvedByAdminId { get; private set; }
        public string? AdminNotes { get; private set; }

        private ContactMessage()
        {
        }

        public static Result<ContactMessage> Create(
            Guid id,
            string name,
            string email,
            string subject,
            string message,
            DateTime createdAtUtc)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<ContactMessage>("Name is required.");
            if (string.IsNullOrWhiteSpace(email))
                return Result.Failure<ContactMessage>("Email is required.");
            if (string.IsNullOrWhiteSpace(subject))
                return Result.Failure<ContactMessage>("Subject is required.");
            if (string.IsNullOrWhiteSpace(message))
                return Result.Failure<ContactMessage>("Message is required.");

            return Result.Success(new ContactMessage
            {
                Id = id,
                Name = name.Trim(),
                Email = email.Trim(),
                Subject = subject.Trim(),
                Message = message.Trim(),
                CreatedAtUtc = createdAtUtc,
                Status = ContactMessageStatus.Open,
                EmailSent = false
            });
        }

        public void MarkEmailSent(bool sent) => EmailSent = sent;

        public Result MarkResolved(Guid adminUserId, string? notes, DateTime utcNow)
        {
            if (Status == ContactMessageStatus.Resolved)
                return Result.Success();

            Status = ContactMessageStatus.Resolved;
            ResolvedAtUtc = utcNow;
            ResolvedByAdminId = adminUserId;
            AdminNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            return Result.Success();
        }

        public Result Reopen()
        {
            Status = ContactMessageStatus.Open;
            ResolvedAtUtc = null;
            ResolvedByAdminId = null;
            return Result.Success();
        }
    }
}
