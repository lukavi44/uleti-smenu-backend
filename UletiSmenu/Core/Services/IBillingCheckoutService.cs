using Core.DTOs;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IBillingCheckoutService
    {
        Task<Result<EmployerCheckoutContext>> GetCheckoutContextAsync(Guid employerId, Guid planId);
        Task<Result<EmployerWalletTopUpContext>> GetWalletTopUpContextAsync(Guid employerId, decimal amount);
        Task<Result> RecordStripeCustomerIdAsync(Guid employerId, string customerId);
        Task<Result<string>> GetStripeCustomerIdAsync(Guid employerId);
    }
}
