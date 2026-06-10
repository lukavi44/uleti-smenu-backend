using Core.Billing;
using Core.DTOs;
using Core.Models.Entities;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;

namespace Infrastructure.Persistence.Services
{
    public class BillingService : IBillingService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;

        public BillingService(
            IUserRepository userRepository,
            ISubscriptionRepository subscriptionRepository)
        {
            _userRepository = userRepository;
            _subscriptionRepository = subscriptionRepository;
        }

        public Result AssignTrialToEmployer(Employer employer)
        {
            var start = DateTime.UtcNow;
            var end = start.AddDays(BillingConstants.TrialDurationDays);
            return employer.AssignSubscription(BillingConstants.TrialPlanId, start, end);
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

        public async Task<Result> ValidateEmployerCanCreatePostAsync(Guid employerId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure("Employer not found.");

            if (!employer.SubscriptionId.HasValue)
                return Result.Failure("No active subscription. Start your free trial or subscribe to post shifts.");

            if (!employer.SubscriptionStart.HasValue || !employer.SubscriptionStop.HasValue)
                return Result.Failure("Subscription data is incomplete.");

            var now = DateTime.UtcNow;
            if (!employer.HasActiveSubscription(now))
                return Result.Failure("Your free trial or subscription has expired. Please subscribe to continue posting.");

            return Result.Success();
        }

        private async Task<EmployerSubscriptionDTO> BuildSubscriptionStatusAsync(Employer employer)
        {
            var now = DateTime.UtcNow;
            var isActive = employer.HasActiveSubscription(now);
            var plan = employer.SubscriptionId.HasValue
                ? await _subscriptionRepository.GetByIdAsync(employer.SubscriptionId.Value)
                : null;

            var status = ResolveStatus(employer, plan, isActive);
            var daysRemaining = 0;
            if (employer.SubscriptionStop.HasValue)
            {
                daysRemaining = Math.Max(0, (int)Math.Ceiling((employer.SubscriptionStop.Value - now).TotalDays));
            }

            return new EmployerSubscriptionDTO
            {
                Status = status,
                PlanTitle = plan?.Title ?? string.Empty,
                SubscriptionStart = employer.SubscriptionStart,
                SubscriptionStop = employer.SubscriptionStop,
                DaysRemaining = daysRemaining,
                IsActive = isActive
            };
        }

        private static BillingPlanDTO MapPlan(Subscription plan)
        {
            var interval = plan.DurationInDays >= 360 ? "year" : "month";
            return new BillingPlanDTO
            {
                Id = plan.Id,
                Title = plan.Title,
                Description = plan.Description,
                Cost = plan.Cost,
                DurationInDays = plan.DurationInDays,
                BillingInterval = interval,
                Currency = "RSD"
            };
        }

        private static string ResolveStatus(Employer employer, Subscription? plan, bool isActive)
        {
            if (!employer.SubscriptionId.HasValue)
                return "None";

            if (!employer.SubscriptionStart.HasValue || !employer.SubscriptionStop.HasValue)
                return "None";

            if (!isActive)
                return "Expired";

            if (plan != null && plan.Cost == 0)
                return "Trial";

            return "Active";
        }
    }
}
