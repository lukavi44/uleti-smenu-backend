using Core.Models.Entities;

namespace Core.Repositories
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetByIdAsync(Guid subscriptionId);
        Task<List<Subscription>> GetPaidPlansAsync();
        Task AddAsync(Subscription subscription);
    }
}
