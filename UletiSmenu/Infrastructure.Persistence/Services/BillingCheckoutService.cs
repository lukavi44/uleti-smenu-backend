using Core.Billing;
using Core.DTOs;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistence.Services
{
    public class BillingCheckoutService : IBillingCheckoutService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly StripeSettings _stripeSettings;

        public BillingCheckoutService(
            IUserRepository userRepository,
            ISubscriptionRepository subscriptionRepository,
            IOptions<StripeSettings> stripeSettings)
        {
            _userRepository = userRepository;
            _subscriptionRepository = subscriptionRepository;
            _stripeSettings = stripeSettings.Value;
        }

        public async Task<Result<EmployerCheckoutContext>> GetCheckoutContextAsync(Guid employerId, Guid planId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure<EmployerCheckoutContext>("Employer not found.");

            var plan = await _subscriptionRepository.GetByIdAsync(planId);
            if (plan == null || plan.PlanKind == PlanKind.Trial)
                return Result.Failure<EmployerCheckoutContext>("Plan is not available for purchase.");

            var priceId = ResolveStripePriceId(plan);
            if (string.IsNullOrWhiteSpace(priceId))
                return Result.Failure<EmployerCheckoutContext>("Stripe price is not configured for this plan.");

            return Result.Success(new EmployerCheckoutContext
            {
                EmployerId = employerId,
                EmployerEmail = employer.Email ?? string.Empty,
                StripeCustomerId = employer.StripeCustomerId,
                PlanId = planId,
                PlanKind = plan.PlanKind,
                StripePriceId = priceId,
                CreditsIncluded = plan.NumberOfPosts,
                CheckoutMode = plan.PlanKind == PlanKind.Basic ? "payment" : "subscription"
            });
        }

        public async Task<Result> RecordStripeCustomerIdAsync(Guid employerId, string customerId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure("Employer not found.");

            employer.UpdateStripeCustomerId(customerId);
            return Result.Success();
        }

        public async Task<Result<string>> GetStripeCustomerIdAsync(Guid employerId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure<string>("Employer not found.");

            if (string.IsNullOrWhiteSpace(employer.StripeCustomerId))
                return Result.Failure<string>("No Stripe customer found.");

            return Result.Success(employer.StripeCustomerId);
        }

        private string ResolveStripePriceId(Subscription plan) => plan.PlanKind switch
        {
            PlanKind.Basic => _stripeSettings.PriceIds.BasicCreditPack,
            PlanKind.Pro => _stripeSettings.PriceIds.ProMonthly,
            _ => string.Empty
        };
    }
}
