namespace Core.DTOs
{
    public class WalletTransactionDTO
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
