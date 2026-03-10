using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IBillingService
    {
        Task<Result> ValidateEmployerCanCreatePostAsync(Guid employerId);
    }
}
