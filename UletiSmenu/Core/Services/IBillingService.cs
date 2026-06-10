using Core.DTOs;
using Core.Models.Entities;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IBillingService
    {
        Result AssignTrialToEmployer(Employer employer);
        Task<EmployerSubscriptionDTO> GetSubscriptionStatusAsync(Guid employerId);
        Task<List<BillingPlanDTO>> GetAvailablePaidPlansAsync();
        Task<Result> ValidateEmployerCanCreatePostAsync(Guid employerId);
    }
}
