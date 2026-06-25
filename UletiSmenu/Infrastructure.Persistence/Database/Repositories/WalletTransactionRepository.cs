using Core.Models.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database.Repositories
{
    public class WalletTransactionRepository : IWalletTransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public WalletTransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsByExternalReferenceAsync(string externalReference)
        {
            return await _context.WalletTransactions
                .AnyAsync(transaction => transaction.ExternalReference == externalReference);
        }

        public async Task AddAsync(WalletTransaction transaction)
        {
            await _context.WalletTransactions.AddAsync(transaction);
        }

        public async Task<List<WalletTransaction>> GetByEmployerIdAsync(Guid employerId, int limit = 50)
        {
            return await _context.WalletTransactions
                .Where(transaction => transaction.EmployerId == employerId)
                .OrderByDescending(transaction => transaction.CreatedAtUtc)
                .Take(limit)
                .ToListAsync();
        }
    }
}
