using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IAccountDeletionService
    {
        Task<Result> DeleteMyAccountAsync(
            Guid userId,
            string password,
            CancellationToken cancellationToken = default);
    }
}
