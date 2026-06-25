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
        private readonly BillingSettings _billingSettings;

        public BillingCheckoutService(
            IUserRepository userRepository,
            ISubscriptionRepository subscriptionRepository,
            IOptions<StripeSettings> stripeSettings,
            IOptions<BillingSettings> billingSettings)
        {
            _userRepository = userRepository;
            _subscriptionRepository = subscriptionRepository;
            _stripeSettings = stripeSettings.Value;
            _billingSettings = billingSettings.Value;
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
                CreditsIncluded = 0,
                CheckoutMode = "subscription"
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

        public async Task<Result<EmployerWalletTopUpContext>> GetWalletTopUpContextAsync(Guid employerId, decimal amount)
        {
            if (amount <= 0)
                return Result.Failure<EmployerWalletTopUpContext>("Top-up amount must be positive.");

            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure<EmployerWalletTopUpContext>("Employer not found.");

            return Result.Success(new EmployerWalletTopUpContext
            {
                EmployerId = employerId,
                EmployerEmail = employer.Email ?? string.Empty,
                StripeCustomerId = employer.StripeCustomerId,
                Amount = amount,
                Currency = _billingSettings.Currency
            });
        }

        private string ResolveStripePriceId(Subscription plan) => plan.PlanKind switch
        {
            PlanKind.Basic => _stripeSettings.PriceIds.BasicMonthly,
            PlanKind.Pro or PlanKind.Unlimited => _stripeSettings.PriceIds.UnlimitedMonthly,
            _ => string.Empty
        };
    }
}
