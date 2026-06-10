using Core.Models.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database.Repositories
{
    public class PaymentEventRepository : IPaymentEventRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentEventRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(string providerEventId)
        {
            return await _context.PaymentEvents.AnyAsync(e => e.ProviderEventId == providerEventId);
        }

        public async Task AddAsync(PaymentEvent paymentEvent)
        {
            await _context.PaymentEvents.AddAsync(paymentEvent);
        }
    }
}
