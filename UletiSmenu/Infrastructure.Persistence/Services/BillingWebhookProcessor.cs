using Core.Billing;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistence.Services
{
    public class BillingWebhookProcessor : IBillingWebhookProcessor
    {
        private readonly IUserRepository _userRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IPaymentEventRepository _paymentEventRepository;
        private readonly IWalletLedgerService _walletLedgerService;
        private readonly IApplicationUnitOfWork _unitOfWork;
        private readonly BillingSettings _billingSettings;
        private readonly StripeSettings _stripeSettings;

        public BillingWebhookProcessor(
            IUserRepository userRepository,
            ISubscriptionRepository subscriptionRepository,
            IPaymentEventRepository paymentEventRepository,
            IWalletLedgerService walletLedgerService,
            IApplicationUnitOfWork unitOfWork,
            IOptions<BillingSettings> billingSettings,
            IOptions<StripeSettings> stripeSettings)
        {
            _userRepository = userRepository;
            _subscriptionRepository = subscriptionRepository;
            _paymentEventRepository = paymentEventRepository;
            _walletLedgerService = walletLedgerService;
            _unitOfWork = unitOfWork;
            _billingSettings = billingSettings.Value;
            _stripeSettings = stripeSettings.Value;
        }

        public async Task<bool> HasProcessedEventAsync(string providerEventId) =>
            await _paymentEventRepository.ExistsAsync(providerEventId);

        public async Task<Result> RecordPaymentEventAsync(string providerEventId, string eventType, string payload)
        {
            if (await _paymentEventRepository.ExistsAsync(providerEventId))
                return Result.Success();

            var paymentEvent = PaymentEvent.Create(
                Guid.NewGuid(),
                providerEventId,
                eventType,
                payload,
                DateTime.UtcNow);

            await _paymentEventRepository.AddAsync(paymentEvent);
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> ProcessCheckoutCompletedAsync(
            Guid employerId,
            Guid planId,
            string customerId,
            string? subscriptionId,
            string? priceId,
            DateTime? periodEndUtc)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure("Employer not found.");

            var plan = await _subscriptionRepository.GetByIdAsync(planId);
            if (plan == null)
                return Result.Failure("Plan not found.");

            employer.UpdateStripeCustomerId(customerId);

            if (plan.PlanKind is PlanKind.Basic or PlanKind.Pro or PlanKind.Unlimited)
            {
                var start = DateTime.UtcNow;
                var end = periodEndUtc ?? start.AddDays(
                    plan.DurationInDays > 0 ? plan.DurationInDays : BillingConstants.MonthlyDurationDays);

                employer.ActivatePaidPlan(
                    planId,
                    start,
                    end,
                    "Stripe",
                    customerId,
                    subscriptionId,
                    priceId);
            }

            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> ProcessWalletTopUpCompletedAsync(
            Guid employerId,
            decimal amount,
            string checkoutSessionId,
            string customerId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure("Employer not found.");

            employer.UpdateStripeCustomerId(customerId);

            var creditResult = await _walletLedgerService.CreditAsync(
                employerId,
                amount,
                WalletTransactionType.TopUp,
                "Wallet top-up via Stripe",
                checkoutSessionId,
                null,
                null);

            if (creditResult.IsFailure)
                return Result.Failure(creditResult.Error);

            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> ProcessSubscriptionUpdatedAsync(
            string subscriptionId,
            BillingStatus status,
            DateTime? periodEndUtc,
            string? priceId)
        {
            var employer = await FindEmployerBySubscriptionIdAsync(subscriptionId);
            if (employer == null)
                return Result.Failure("Employer not found for subscription.");

            employer.SyncStripeSubscription(
                status,
                periodEndUtc,
                subscriptionId,
                priceId,
                _billingSettings.GracePeriodDays);

            if (status == BillingStatus.Active && priceId != null)
            {
                var plan = await ResolvePlanByPriceIdAsync(priceId);
                if (plan != null)
                {
                    var start = employer.SubscriptionStart ?? DateTime.UtcNow;
                    var end = periodEndUtc ?? start.AddDays(
                        plan.DurationInDays > 0 ? plan.DurationInDays : BillingConstants.MonthlyDurationDays);

                    employer.ActivatePaidPlan(
                        plan.Id,
                        start,
                        end,
                        "Stripe",
                        employer.StripeCustomerId,
                        subscriptionId,
                        priceId);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> ProcessSubscriptionDeletedAsync(string subscriptionId)
        {
            var employer = await FindEmployerBySubscriptionIdAsync(subscriptionId);
            if (employer == null)
                return Result.Success();

            employer.SyncStripeSubscription(
                BillingStatus.Canceled,
                employer.CurrentPeriodEndUtc,
                subscriptionId,
                employer.StripePriceId,
                _billingSettings.GracePeriodDays);

            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> ProcessInvoicePaymentFailedAsync(string subscriptionId)
        {
            var employer = await FindEmployerBySubscriptionIdAsync(subscriptionId);
            if (employer == null)
                return Result.Success();

            employer.SyncStripeSubscription(
                BillingStatus.PastDue,
                employer.CurrentPeriodEndUtc,
                subscriptionId,
                employer.StripePriceId,
                _billingSettings.GracePeriodDays);

            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> ProcessInvoicePaymentSucceededAsync(string subscriptionId)
        {
            var employer = await FindEmployerBySubscriptionIdAsync(subscriptionId);
            if (employer == null)
                return Result.Success();

            employer.SyncStripeSubscription(
                BillingStatus.Active,
                employer.CurrentPeriodEndUtc,
                subscriptionId,
                employer.StripePriceId,
                _billingSettings.GracePeriodDays);

            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        private async Task<Employer?> FindEmployerBySubscriptionIdAsync(string subscriptionId)
        {
            return await _userRepository.FindEmployerByStripeSubscriptionIdAsync(subscriptionId);
        }

        private async Task<Subscription?> ResolvePlanByPriceIdAsync(string priceId)
        {
            var plans = await _subscriptionRepository.GetPaidPlansAsync();
            foreach (var plan in plans)
            {
                var configuredPriceId = ResolveStripePriceId(plan);
                if (!string.IsNullOrWhiteSpace(configuredPriceId) &&
                    string.Equals(configuredPriceId, priceId, StringComparison.Ordinal))
                {
                    return plan;
                }
            }

            return null;
        }

        private string ResolveStripePriceId(Subscription plan) => plan.PlanKind switch
        {
            PlanKind.Basic => _stripeSettings.PriceIds.BasicMonthly,
            PlanKind.Pro or PlanKind.Unlimited => _stripeSettings.PriceIds.UnlimitedMonthly,
            _ => string.Empty
        };
    }
}
