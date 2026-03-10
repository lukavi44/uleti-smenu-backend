using Core.Models.Entities;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;

namespace Infrastructure.Persistence.Services
{
    public class BillingService : IBillingService
    {
        private readonly IUserRepository _userRepository;

        public BillingService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result> ValidateEmployerCanCreatePostAsync(Guid employerId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure("Employer not found.");

            // Temporary policy until full billing implementation:
            // - no subscription => per-post payment flow will handle charging later.
            // - has subscription => it must currently be active.
            if (!employer.SubscriptionId.HasValue)
                return Result.Success();

            if (!employer.SubscriptionStart.HasValue || !employer.SubscriptionStop.HasValue)
                return Result.Failure("Subscription data is incomplete.");

            var now = DateTime.UtcNow;
            if (employer.SubscriptionStart.Value > now || employer.SubscriptionStop.Value < now)
                return Result.Failure("Subscription is not active.");

            return Result.Success();
        }
    }
}
