namespace Core.DTOs
{
    public class EmployerWalletTopUpContext
    {
        public Guid EmployerId { get; set; }
        public string EmployerEmail { get; set; } = string.Empty;
        public string? StripeCustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RSD";
    }
}
