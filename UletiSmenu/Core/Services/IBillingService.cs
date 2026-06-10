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
        Task<Result> OnJobPostCreatedAsync(Guid employerId);
        Task<Result<string>> CreateCheckoutSessionAsync(Guid employerId, Guid planId, string successUrl, string cancelUrl);
        Task<Result<string>> CreateCustomerPortalSessionAsync(Guid employerId, string returnUrl);
        bool IsPaymentsEnabled();
    }
}
