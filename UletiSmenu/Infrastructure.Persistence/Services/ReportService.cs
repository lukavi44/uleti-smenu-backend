using Core.Models.Entities;
using Core.Models.Enums;
using Core.Services;
using CSharpFunctionalExtensions;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result> SubmitAsync(
            Guid reporterUserId,
            ReportTargetType targetType,
            Guid targetId,
            string reason,
            string? details,
            CancellationToken cancellationToken = default)
        {
            var targetExists = targetType switch
            {
                ReportTargetType.JobPost => await _context.JobPosts.AnyAsync(post => post.Id == targetId, cancellationToken),
                ReportTargetType.Employer => await _context.Users.OfType<Employer>().AnyAsync(employer => employer.Id == targetId, cancellationToken),
                _ => false
            };

            if (!targetExists)
                return Result.Failure("Reported item was not found.");

            var duplicateOpen = await _context.ModerationReports.AnyAsync(
                report =>
                    report.ReporterUserId == reporterUserId &&
                    report.TargetType == targetType &&
                    report.TargetId == targetId &&
                    report.Status == ReportStatus.Open,
                cancellationToken);

            if (duplicateOpen)
                return Result.Failure("You already have an open report for this item.");

            var createResult = ModerationReport.Create(
                Guid.NewGuid(),
                reporterUserId,
                targetType,
                targetId,
                reason,
                details,
                DateTime.UtcNow);

            if (createResult.IsFailure)
                return Result.Failure(createResult.Error);

            _context.ModerationReports.Add(createResult.Value);
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
