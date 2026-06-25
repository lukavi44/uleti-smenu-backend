namespace API.DTOs
{
    public class BillingCheckoutRequest
    {
        public Guid PlanId { get; set; }
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class BillingPortalRequest
    {
        public string ReturnUrl { get; set; } = string.Empty;
    }

    public class BillingWalletTopUpRequest
    {
        public decimal Amount { get; set; }
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }
}
