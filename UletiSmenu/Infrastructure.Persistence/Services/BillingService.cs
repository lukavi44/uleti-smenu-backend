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
        private readonly IWalletTransactionRepository _walletTransactionRepository;
        private readonly IPaymentProvider _paymentProvider;
        private readonly IWalletLedgerService _walletLedgerService;
        private readonly IApplicationUnitOfWork _unitOfWork;
        private readonly BillingSettings _billingSettings;

        public BillingService(
            IUserRepository userRepository,
            ISubscriptionRepository subscriptionRepository,
            IJobPostRepository jobPostRepository,
            IWalletTransactionRepository walletTransactionRepository,
            IPaymentProvider paymentProvider,
            IWalletLedgerService walletLedgerService,
            IApplicationUnitOfWork unitOfWork,
            IOptions<BillingSettings> billingSettings)
        {
            _userRepository = userRepository;
            _subscriptionRepository = subscriptionRepository;
            _jobPostRepository = jobPostRepository;
            _walletTransactionRepository = walletTransactionRepository;
            _paymentProvider = paymentProvider;
            _walletLedgerService = walletLedgerService;
            _unitOfWork = unitOfWork;
            _billingSettings = billingSettings.Value;
        }

        public Result GrantRegistrationBonus(Employer employer)
        {
            employer.GrantRegistrationBonus(_billingSettings.RegistrationFreeCredits);
            return Result.Success();
        }

        public async Task<EmployerSubscriptionDTO> GetSubscriptionStatusAsync(Guid employerId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
            {
                return new EmployerSubscriptionDTO
                {
                    Status = "None",
                    Currency = _billingSettings.Currency,
                    JobPostPrice = _billingSettings.JobPostPrice,
                    IsActive = false,
                    CanPost = false
                };
            }

            return await BuildSubscriptionStatusAsync(employer);
        }

        public async Task<List<BillingPlanDTO>> GetAvailablePaidPlansAsync()
        {
            var plans = await _subscriptionRepository.GetPaidPlansAsync();
            return plans.Select(MapPlan).ToList();
        }

        public async Task<List<WalletTransactionDTO>> GetWalletTransactionsAsync(Guid employerId, int limit = 50)
        {
            var transactions = await _walletTransactionRepository.GetByEmployerIdAsync(employerId, limit);
            return transactions.Select(transaction => new WalletTransactionDTO
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                BalanceAfter = transaction.BalanceAfter,
                Type = transaction.Type.ToString(),
                Description = transaction.Description,
                CreatedAtUtc = transaction.CreatedAtUtc
            }).ToList();
        }

        public bool IsPaymentsEnabled() => _paymentProvider.IsEnabled;

        public decimal[] GetSuggestedTopUpAmounts() => _billingSettings.SuggestedTopUpAmounts;

        public int GetRegistrationFreeCredits() => _billingSettings.RegistrationFreeCredits;

        public Task<Result<string>> CreateCheckoutSessionAsync(
            Guid employerId,
            Guid planId,
            string successUrl,
            string cancelUrl) =>
            _paymentProvider.CreateCheckoutSessionAsync(employerId, planId, successUrl, cancelUrl);

        public Task<Result<string>> CreateWalletTopUpCheckoutSessionAsync(
            Guid employerId,
            decimal amount,
            string successUrl,
            string cancelUrl) =>
            _paymentProvider.CreateWalletTopUpCheckoutSessionAsync(employerId, amount, successUrl, cancelUrl);

        public Task<Result<string>> CreateCustomerPortalSessionAsync(Guid employerId, string returnUrl) =>
            _paymentProvider.CreateCustomerPortalSessionAsync(employerId, returnUrl);

        public async Task<Result> ValidateEmployerCanCreatePostAsync(Guid employerId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure("Employer not found.");

            var decision = await ResolvePostingChargeAsync(employer);
            if (!decision.CanPost)
                return Result.Failure(decision.Reason);

            return Result.Success();
        }

        public async Task<Result> OnJobPostCreatedAsync(Guid employerId, Guid jobPostId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure("Employer not found.");

            var decision = await ResolvePostingChargeAsync(employer);
            if (!decision.CanPost)
                return Result.Failure(decision.Reason);

            Result chargeResult = decision.Source switch
            {
                PostingChargeSource.FreeCredit => employer.ConsumePostCredit(1),
                PostingChargeSource.Wallet => await ChargeWalletForJobPostAsync(employerId, jobPostId),
                PostingChargeSource.UnlimitedSubscription or PostingChargeSource.BasicSubscription => Result.Success(),
                _ => Result.Failure("Unable to determine posting charge.")
            };

            if (chargeResult.IsFailure)
                return chargeResult;

            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        private async Task<Result> ChargeWalletForJobPostAsync(Guid employerId, Guid jobPostId)
        {
            var debitResult = await _walletLedgerService.DebitAsync(
                employerId,
                _billingSettings.JobPostPrice,
                WalletTransactionType.JobPostCharge,
                "Job post publication",
                null,
                "JobPost",
                jobPostId);

            if (debitResult.IsFailure)
                return Result.Failure(debitResult.Error);

            return Result.Success();
        }

        private async Task<PostingChargeDecision> ResolvePostingChargeAsync(Employer employer)
        {
            if (employer.PostCredits > 0)
            {
                return new PostingChargeDecision(
                    true,
                    PostingChargeSource.FreeCredit,
                    string.Empty);
            }

            var plan = employer.SubscriptionId.HasValue
                ? await _subscriptionRepository.GetByIdAsync(employer.SubscriptionId.Value)
                : null;

            var now = DateTime.UtcNow;
            var activePosts = await CountActivePostsAsync(employer.Id);

            if (plan != null && IsUnlimitedPlan(plan.PlanKind) && HasSubscriptionPostingAccess(employer, now))
            {
                return new PostingChargeDecision(
                    true,
                    PostingChargeSource.UnlimitedSubscription,
                    string.Empty);
            }

            if (plan != null && plan.PlanKind == PlanKind.Basic && HasSubscriptionPostingAccess(employer, now))
            {
                var maxActive = GetMaxActivePosts(PlanKind.Basic);
                if (activePosts >= maxActive)
                {
                    return new PostingChargeDecision(
                        false,
                        null,
                        $"You have reached the limit of {maxActive} active job posts for your Basic plan.");
                }

                return new PostingChargeDecision(
                    true,
                    PostingChargeSource.BasicSubscription,
                    string.Empty);
            }

            if (employer.WalletBalance >= _billingSettings.JobPostPrice)
            {
                return new PostingChargeDecision(
                    true,
                    PostingChargeSource.Wallet,
                    string.Empty);
            }

            return new PostingChargeDecision(
                false,
                null,
                "You need free credits, an active subscription, or sufficient wallet balance to post. Visit billing to top up or subscribe.");
        }

        private async Task<EmployerSubscriptionDTO> BuildSubscriptionStatusAsync(Employer employer)
        {
            var now = DateTime.UtcNow;
            var plan = employer.SubscriptionId.HasValue
                ? await _subscriptionRepository.GetByIdAsync(employer.SubscriptionId.Value)
                : null;

            var activePosts = await CountActivePostsAsync(employer.Id);
            var decision = await ResolvePostingChargeAsync(employer);
            var status = ResolveDisplayStatus(employer, plan, now);
            var subscriptionActive = plan != null && HasSubscriptionPostingAccess(employer, now);

            var periodEnd = employer.CurrentPeriodEndUtc ?? employer.SubscriptionStop;
            var daysRemaining = 0;
            if (periodEnd.HasValue)
                daysRemaining = Math.Max(0, (int)Math.Ceiling((periodEnd.Value - now).TotalDays));

            var needsAttention = employer.BillingStatus is BillingStatus.PastDue or BillingStatus.Canceled
                && subscriptionActive;

            return new EmployerSubscriptionDTO
            {
                Status = status,
                PlanTitle = plan?.Title ?? string.Empty,
                PlanKind = plan?.PlanKind.ToString() ?? string.Empty,
                SubscriptionStart = employer.SubscriptionStart,
                SubscriptionStop = employer.SubscriptionStop,
                GracePeriodEndsAtUtc = employer.GracePeriodEndsAtUtc,
                DaysRemaining = daysRemaining,
                FreePostingCredits = employer.PostCredits,
                PostCredits = employer.PostCredits,
                WalletBalance = employer.WalletBalance,
                Currency = _billingSettings.Currency,
                JobPostPrice = _billingSettings.JobPostPrice,
                ActiveJobPostsCount = activePosts,
                MaxActivePosts = plan != null ? GetMaxActivePosts(NormalizePlanKind(plan.PlanKind)) : 0,
                NextPostingChargeSource = decision.Source?.ToString(),
                IsActive = subscriptionActive || employer.PostCredits > 0 || employer.WalletBalance > 0,
                CanPost = decision.CanPost,
                NeedsAttention = needsAttention,
                CanManageBilling = !string.IsNullOrWhiteSpace(employer.StripeCustomerId) && _paymentProvider.IsEnabled
            };
        }

        private static BillingPlanDTO MapPlan(Subscription plan)
        {
            var normalizedKind = NormalizePlanKind(plan.PlanKind);

            return new BillingPlanDTO
            {
                Id = plan.Id,
                Title = plan.Title,
                Description = plan.Description,
                Cost = plan.Cost,
                DurationInDays = plan.DurationInDays,
                CreditsIncluded = 0,
                BillingInterval = "month",
                PlanKind = normalizedKind.ToString(),
                CheckoutMode = "subscription",
                Currency = "RSD"
            };
        }

        private static bool IsUnlimitedPlan(PlanKind planKind) =>
            planKind is PlanKind.Unlimited or PlanKind.Pro;

        private static PlanKind NormalizePlanKind(PlanKind planKind) =>
            planKind == PlanKind.Pro ? PlanKind.Unlimited : planKind;

        private bool HasSubscriptionPostingAccess(Employer employer, DateTime now)
        {
            if (!employer.SubscriptionId.HasValue)
                return false;

            return employer.BillingStatus switch
            {
                BillingStatus.Active => employer.HasActiveSubscription(now) ||
                                        (employer.CurrentPeriodEndUtc.HasValue && now <= employer.CurrentPeriodEndUtc.Value),
                BillingStatus.PastDue => employer.CanPostDuringPastDue(now),
                BillingStatus.Canceled =>
                    (employer.CurrentPeriodEndUtc ?? employer.SubscriptionStop).HasValue &&
                    now <= (employer.CurrentPeriodEndUtc ?? employer.SubscriptionStop)!.Value,
                _ => false
            };
        }

        private static string ResolveDisplayStatus(Employer employer, Subscription? plan, DateTime now)
        {
            if (!employer.SubscriptionId.HasValue || plan == null)
                return "None";

            if (employer.BillingStatus == BillingStatus.PastDue)
                return "PastDue";

            if (employer.BillingStatus == BillingStatus.Canceled)
                return "Canceled";

            if (employer.BillingStatus == BillingStatus.Incomplete)
                return "Incomplete";

            if (employer.BillingStatus == BillingStatus.Expired)
                return "Expired";

            if (employer.BillingStatus == BillingStatus.Active)
                return "Active";

            if (!employer.HasActiveSubscription(now))
                return "Expired";

            return "Active";
        }

        private int GetMaxActivePosts(PlanKind planKind)
        {
            var normalized = NormalizePlanKind(planKind);
            var settings = normalized switch
            {
                PlanKind.Basic => _billingSettings.Basic,
                PlanKind.Unlimited => _billingSettings.Unlimited,
                _ => null
            };

            if (settings == null || settings.MaxActivePosts < 0)
                return int.MaxValue;

            return settings.MaxActivePosts;
        }

        private Task<int> CountActivePostsAsync(Guid employerId) =>
            _jobPostRepository.CountActiveByEmployerIdAsync(employerId);

        private sealed record PostingChargeDecision(
            bool CanPost,
            PostingChargeSource? Source,
            string Reason);
    }
}
