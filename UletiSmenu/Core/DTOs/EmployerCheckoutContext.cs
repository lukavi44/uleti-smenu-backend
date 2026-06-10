using Core.Models.Enums;

namespace Core.DTOs
{
    public class EmployerCheckoutContext
    {
        public Guid EmployerId { get; set; }
        public string EmployerEmail { get; set; } = string.Empty;
        public string? StripeCustomerId { get; set; }
        public Guid PlanId { get; set; }
        public PlanKind PlanKind { get; set; }
        public string StripePriceId { get; set; } = string.Empty;
        public int CreditsIncluded { get; set; }
        public string CheckoutMode { get; set; } = "subscription";
    }
}
