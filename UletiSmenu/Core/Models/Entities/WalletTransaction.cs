using Core.Models.Enums;

namespace Core.Models.Entities
{
    public class WalletTransaction
    {
        public Guid Id { get; private set; }
        public Guid EmployerId { get; private set; }
        public decimal Amount { get; private set; }
        public decimal BalanceAfter { get; private set; }
        public WalletTransactionType Type { get; private set; }
        public string? Description { get; private set; }
        public string? ExternalReference { get; private set; }
        public string? RelatedEntityType { get; private set; }
        public Guid? RelatedEntityId { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        private WalletTransaction() { }

        public static WalletTransaction Create(
            Guid id,
            Guid employerId,
            decimal amount,
            decimal balanceAfter,
            WalletTransactionType type,
            string? description,
            string? externalReference,
            string? relatedEntityType,
            Guid? relatedEntityId,
            DateTime createdAtUtc)
        {
            return new WalletTransaction
            {
                Id = id,
                EmployerId = employerId,
                Amount = amount,
                BalanceAfter = balanceAfter,
                Type = type,
                Description = description,
                ExternalReference = externalReference,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                CreatedAtUtc = createdAtUtc
            };
        }
    }
}
