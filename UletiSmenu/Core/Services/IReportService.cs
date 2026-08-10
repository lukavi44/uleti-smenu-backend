using Core.Models.Enums;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IReportService
    {
        Task<Result> SubmitAsync(
            Guid reporterUserId,
            ReportTargetType targetType,
            Guid targetId,
            string reason,
            string? details,
            CancellationToken cancellationToken = default);
    }
}
