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
    public class BillingService : IBillingService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IJobPostRepository _jobPostRepository;
        private readonly IPaymentProvider _paymentProvider;
        private readonly BillingSettings _billingSettings;

        public BillingService(
            IUserRepository userRepository,
            ISubscriptionRepository subscriptionRepository,
            IJobPostRepository jobPostRepository,
            IPaymentProvider paymentProvider,
            IOptions<BillingSettings> billingSettings)
        {
            _userRepository = userRepository;
            _subscriptionRepository = subscriptionRepository;
            _jobPostRepository = jobPostRepository;
            _paymentProvider = paymentProvider;
            _billingSettings = billingSettings.Value;
        }

        public Result AssignTrialToEmployer(Employer employer)
        {
            var start = DateTime.UtcNow;
            var end = start.AddDays(BillingConstants.TrialDurationDays);
            return employer.AssignTrial(BillingConstants.TrialPlanId, start, end);
        }

        public async Task<EmployerSubscriptionDTO> GetSubscriptionStatusAsync(Guid employerId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
            {
                return new EmployerSubscriptionDTO
                {
                    Status = "None",
                    IsActive = false
                };
            }

            return await BuildSubscriptionStatusAsync(employer);
        }

        public async Task<List<BillingPlanDTO>> GetAvailablePaidPlansAsync()
        {
            var plans = await _subscriptionRepository.GetPaidPlansAsync();
            return plans.Select(MapPlan).ToList();
        }

        public bool IsPaymentsEnabled() => _paymentProvider.IsEnabled;

        public Task<Result<string>> CreateCheckoutSessionAsync(
            Guid employerId,
            Guid planId,
            string successUrl,
            string cancelUrl) =>
            _paymentProvider.CreateCheckoutSessionAsync(employerId, planId, successUrl, cancelUrl);

        public Task<Result<string>> CreateCustomerPortalSessionAsync(Guid employerId, string returnUrl) =>
            _paymentProvider.CreateCustomerPortalSessionAsync(employerId, returnUrl);

        public async Task<Result> ValidateEmployerCanCreatePostAsync(Guid employerId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure("Employer not found.");

            if (!employer.SubscriptionId.HasValue)
                return Result.Failure("No active subscription. Start your free trial or subscribe to post shifts.");

            var plan = await _subscriptionRepository.GetByIdAsync(employer.SubscriptionId.Value);
            if (plan == null)
                return Result.Failure("Subscription plan not found.");

            var now = DateTime.UtcNow;
            var access = EvaluatePostingAccess(employer, plan, now);
            if (!access.CanPost)
                return Result.Failure(access.Reason);

            var activePosts = await CountActivePostsAsync(employerId);
            var maxActive = GetMaxActivePosts(plan.PlanKind);
            if (activePosts >= maxActive)
                return Result.Failure($"You have reached the limit of {maxActive} active job posts for your plan.");

            if (plan.PlanKind == PlanKind.Basic)
            {
                var creditsRequired = GetCreditsPerPost(plan.PlanKind);
                if (employer.PostCredits < creditsRequired)
                    return Result.Failure("You need post credits to publish. Buy a credit pack on the billing page.");
            }

            return Result.Success();
        }

        public async Task<Result> OnJobPostCreatedAsync(Guid employerId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure("Employer not found.");

            if (!employer.SubscriptionId.HasValue)
                return Result.Success();

            var plan = await _subscriptionRepository.GetByIdAsync(employer.SubscriptionId.Value);
            if (plan?.PlanKind != PlanKind.Basic)
                return Result.Success();

            var creditsRequired = GetCreditsPerPost(PlanKind.Basic);
            return employer.ConsumePostCredit(creditsRequired);
        }

        private Task<int> CountActivePostsAsync(Guid employerId) =>
            _jobPostRepository.CountActiveByEmployerIdAsync(employerId);

        private async Task<EmployerSubscriptionDTO> BuildSubscriptionStatusAsync(Employer employer)
        {
            var now = DateTime.UtcNow;
            var plan = employer.SubscriptionId.HasValue
                ? await _subscriptionRepository.GetByIdAsync(employer.SubscriptionId.Value)
                : null;

            var status = ResolveDisplayStatus(employer, plan, now);
            var access = plan != null
                ? EvaluatePostingAccess(employer, plan, now)
                : new PostingAccess(false, string.Empty);

            var periodEnd = employer.CurrentPeriodEndUtc ?? employer.SubscriptionStop;
            var daysRemaining = 0;
            if (periodEnd.HasValue)
                daysRemaining = Math.Max(0, (int)Math.Ceiling((periodEnd.Value - now).TotalDays));

            var needsAttention = employer.BillingStatus is BillingStatus.PastDue or BillingStatus.Canceled
                || (employer.BillingStatus == BillingStatus.Trialing && daysRemaining <= 14);

            return new EmployerSubscriptionDTO
            {
                Status = status,
                PlanTitle = plan?.Title ?? string.Empty,
                PlanKind = plan?.PlanKind.ToString() ?? string.Empty,
                SubscriptionStart = employer.SubscriptionStart,
                SubscriptionStop = employer.SubscriptionStop,
                GracePeriodEndsAtUtc = employer.GracePeriodEndsAtUtc,
                DaysRemaining = daysRemaining,
                PostCredits = employer.PostCredits,
                MaxActivePosts = plan != null ? GetMaxActivePosts(plan.PlanKind) : 0,
                IsActive = access.CanPost || employer.BillingStatus is BillingStatus.Trialing or BillingStatus.Active or BillingStatus.PastDue,
                NeedsAttention = needsAttention,
                CanManageBilling = !string.IsNullOrWhiteSpace(employer.StripeCustomerId) && _paymentProvider.IsEnabled
            };
        }

        private static BillingPlanDTO MapPlan(Subscription plan)
        {
            var interval = plan.PlanKind switch
            {
                PlanKind.Basic => "pack",
                PlanKind.Pro when plan.DurationInDays >= 360 => "year",
                PlanKind.Pro => "month",
                _ => "trial"
            };

            var checkoutMode = plan.PlanKind == PlanKind.Basic ? "payment" : "subscription";

            return new BillingPlanDTO
            {
                Id = plan.Id,
                Title = plan.Title,
                Description = plan.Description,
                Cost = plan.Cost,
                DurationInDays = plan.DurationInDays,
                CreditsIncluded = plan.NumberOfPosts,
                BillingInterval = interval,
                PlanKind = plan.PlanKind.ToString(),
                CheckoutMode = checkoutMode,
                Currency = "EUR"
            };
        }

        private PostingAccess EvaluatePostingAccess(Employer employer, Subscription plan, DateTime now)
        {
            switch (employer.BillingStatus)
            {
                case BillingStatus.Trialing:
                    if (employer.TrialEndsAtUtc.HasValue && now > employer.TrialEndsAtUtc.Value)
                        return new PostingAccess(false, "Your free trial has expired. Please upgrade to continue posting.");
                    if (!employer.HasActiveSubscription(now))
                        return new PostingAccess(false, "Your free trial has expired. Please upgrade to continue posting.");
                    return new PostingAccess(true, string.Empty);

                case BillingStatus.Active:
                    if (plan.PlanKind == PlanKind.Pro && employer.CurrentPeriodEndUtc.HasValue && now > employer.CurrentPeriodEndUtc.Value)
                        return new PostingAccess(false, "Your subscription period has ended. Please renew to post new shifts.");
                    return new PostingAccess(true, string.Empty);

                case BillingStatus.PastDue:
                    if (employer.CanPostDuringPastDue(now))
                        return new PostingAccess(true, string.Empty);
                    return new PostingAccess(false, "Your subscription needs attention. Update billing to post new shifts.");

                case BillingStatus.Canceled:
                    var accessUntil = employer.CurrentPeriodEndUtc ?? employer.SubscriptionStop;
                    if (accessUntil.HasValue && now <= accessUntil.Value)
                        return new PostingAccess(true, string.Empty);
                    return new PostingAccess(false, "Your subscription has ended. Please subscribe again to post new shifts.");

                case BillingStatus.Expired:
                case BillingStatus.Incomplete:
                default:
                    return new PostingAccess(false, "Your free trial or subscription has expired. Please subscribe to continue posting.");
            }
        }

        private static string ResolveDisplayStatus(Employer employer, Subscription? plan, DateTime now)
        {
            if (!employer.SubscriptionId.HasValue)
                return "None";

            if (employer.BillingStatus == BillingStatus.PastDue)
                return "PastDue";

            if (employer.BillingStatus == BillingStatus.Canceled)
                return "Canceled";

            if (employer.BillingStatus == BillingStatus.Incomplete)
                return "Incomplete";

            if (employer.BillingStatus == BillingStatus.Expired)
                return "Expired";

            if (employer.BillingStatus == BillingStatus.Trialing)
                return "Trialing";

            if (employer.BillingStatus == BillingStatus.Active)
                return "Active";

            if (!employer.HasActiveSubscription(now))
                return "Expired";

            if (plan != null && plan.PlanKind == PlanKind.Trial)
                return "Trialing";

            return "Active";
        }

        private int GetMaxActivePosts(PlanKind planKind) => planKind switch
        {
            PlanKind.Trial => _billingSettings.Trial.MaxActivePosts,
            PlanKind.Basic => _billingSettings.Basic.MaxActivePosts,
            PlanKind.Pro => _billingSettings.Pro.MaxActivePosts,
            _ => 0
        };

        private int GetCreditsPerPost(PlanKind planKind) => planKind switch
        {
            PlanKind.Basic => _billingSettings.Basic.CreditsPerPost,
            _ => 0
        };

        private sealed record PostingAccess(bool CanPost, string Reason);
    }
}
