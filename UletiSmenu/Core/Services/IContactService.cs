using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IContactService
    {
        Task<Result> SubmitAsync(
            string name,
            string email,
            string subject,
            string message,
            CancellationToken cancellationToken = default);
    }
}
