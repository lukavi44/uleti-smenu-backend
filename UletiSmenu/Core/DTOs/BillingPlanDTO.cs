namespace Core.DTOs
{
    public class BillingPlanDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public int DurationInDays { get; set; }
        public int CreditsIncluded { get; set; }
        public string BillingInterval { get; set; } = string.Empty;
        public string PlanKind { get; set; } = string.Empty;
        public string CheckoutMode { get; set; } = "subscription";
        public string Currency { get; set; } = "EUR";
    }
}
