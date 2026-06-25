using Core.Models.Entities;
using Core.Models.Enums;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IWalletLedgerService
    {
        Task<Result<WalletTransaction>> CreditAsync(
            Guid employerId,
            decimal amount,
            WalletTransactionType type,
            string? description,
            string? externalReference,
            string? relatedEntityType,
            Guid? relatedEntityId);

        Task<Result<WalletTransaction>> DebitAsync(
            Guid employerId,
            decimal amount,
            WalletTransactionType type,
            string? description,
            string? externalReference,
            string? relatedEntityType,
            Guid? relatedEntityId);
    }
}
