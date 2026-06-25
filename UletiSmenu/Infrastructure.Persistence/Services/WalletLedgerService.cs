using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;

namespace Infrastructure.Persistence.Services
{
    public class WalletLedgerService : IWalletLedgerService
    {
        private readonly IUserRepository _userRepository;
        private readonly IWalletTransactionRepository _walletTransactionRepository;

        public WalletLedgerService(
            IUserRepository userRepository,
            IWalletTransactionRepository walletTransactionRepository)
        {
            _userRepository = userRepository;
            _walletTransactionRepository = walletTransactionRepository;
        }

        public async Task<Result<WalletTransaction>> CreditAsync(
            Guid employerId,
            decimal amount,
            WalletTransactionType type,
            string? description,
            string? externalReference,
            string? relatedEntityType,
            Guid? relatedEntityId)
        {
            if (amount <= 0)
                return Result.Failure<WalletTransaction>("Credit amount must be positive.");

            if (!string.IsNullOrWhiteSpace(externalReference) &&
                await _walletTransactionRepository.ExistsByExternalReferenceAsync(externalReference))
            {
                return Result.Failure<WalletTransaction>("Wallet transaction already processed.");
            }

            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure<WalletTransaction>("Employer not found.");

            var creditResult = employer.CreditWallet(amount);
            if (creditResult.IsFailure)
                return Result.Failure<WalletTransaction>(creditResult.Error);

            var transaction = WalletTransaction.Create(
                Guid.NewGuid(),
                employerId,
                amount,
                employer.WalletBalance,
                type,
                description,
                externalReference,
                relatedEntityType,
                relatedEntityId,
                DateTime.UtcNow);

            await _walletTransactionRepository.AddAsync(transaction);
            return Result.Success(transaction);
        }

        public async Task<Result<WalletTransaction>> DebitAsync(
            Guid employerId,
            decimal amount,
            WalletTransactionType type,
            string? description,
            string? externalReference,
            string? relatedEntityType,
            Guid? relatedEntityId)
        {
            if (amount <= 0)
                return Result.Failure<WalletTransaction>("Debit amount must be positive.");

            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure<WalletTransaction>("Employer not found.");

            var debitResult = employer.DebitWallet(amount);
            if (debitResult.IsFailure)
                return Result.Failure<WalletTransaction>(debitResult.Error);

            var transaction = WalletTransaction.Create(
                Guid.NewGuid(),
                employerId,
                -amount,
                employer.WalletBalance,
                type,
                description,
                externalReference,
                relatedEntityType,
                relatedEntityId,
                DateTime.UtcNow);

            await _walletTransactionRepository.AddAsync(transaction);
            return Result.Success(transaction);
        }
    }
}
