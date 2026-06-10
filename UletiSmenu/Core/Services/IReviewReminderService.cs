using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IReviewReminderService
    {
        Task<Result> SyncReviewRemindersAsync(Guid userId, string role);
    }
}
