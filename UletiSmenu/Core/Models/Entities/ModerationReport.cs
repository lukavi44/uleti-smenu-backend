using Core.Models.Enums;
using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class ModerationReport
    {
        public Guid Id { get; private set; }
        public Guid ReporterUserId { get; private set; }
        public ReportTargetType TargetType { get; private set; }
        public Guid TargetId { get; private set; }
        public string Reason { get; private set; } = string.Empty;
        public string? Details { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public ReportStatus Status { get; private set; }
        public DateTime? ResolvedAtUtc { get; private set; }
        public Guid? ResolvedByAdminId { get; private set; }
        public string? AdminNotes { get; private set; }

        private ModerationReport()
        {
        }

        public static Result<ModerationReport> Create(
            Guid id,
            Guid reporterUserId,
            ReportTargetType targetType,
            Guid targetId,
            string reason,
            string? details,
            DateTime createdAtUtc)
        {
            if (reporterUserId == Guid.Empty)
                return Result.Failure<ModerationReport>("Reporter is required.");
            if (targetId == Guid.Empty)
                return Result.Failure<ModerationReport>("Target is required.");
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure<ModerationReport>("Reason is required.");

            var trimmedReason = reason.Trim();
            if (trimmedReason.Length > 80)
                return Result.Failure<ModerationReport>("Reason is too long.");

            var trimmedDetails = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
            if (trimmedDetails != null && trimmedDetails.Length > 2000)
                return Result.Failure<ModerationReport>("Details are too long.");

            return Result.Success(new ModerationReport
            {
                Id = id,
                ReporterUserId = reporterUserId,
                TargetType = targetType,
                TargetId = targetId,
                Reason = trimmedReason,
                Details = trimmedDetails,
                CreatedAtUtc = createdAtUtc,
                Status = ReportStatus.Open
            });
        }

        public Result MarkResolved(Guid adminUserId, string? notes, DateTime utcNow)
        {
            if (Status == ReportStatus.Resolved)
                return Result.Success();

            Status = ReportStatus.Resolved;
            ResolvedAtUtc = utcNow;
            ResolvedByAdminId = adminUserId;
            AdminNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            return Result.Success();
        }
    }
}
