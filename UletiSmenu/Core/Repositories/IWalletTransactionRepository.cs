using Core.Models.Entities;
using Core.Models.Enums;

namespace Core.Repositories
{
    public interface IWalletTransactionRepository
    {
        Task<bool> ExistsByExternalReferenceAsync(string externalReference);
        Task AddAsync(WalletTransaction transaction);
        Task<List<WalletTransaction>> GetByEmployerIdAsync(Guid employerId, int limit = 50);
    }
}
