using Core.Models.Entities;

namespace Core.Repositories
{
    public interface IPaymentEventRepository
    {
        Task<bool> ExistsAsync(string providerEventId);
        Task AddAsync(PaymentEvent paymentEvent);
    }
}
