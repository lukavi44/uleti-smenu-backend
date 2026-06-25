namespace Core.Billing
{
    public class StripeSettings
    {
        public const string SectionName = "Stripe";

        public bool Enabled { get; set; }
        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public StripePriceIds PriceIds { get; set; } = new();
    }

    public class StripePriceIds
    {
        public string BasicMonthly { get; set; } = string.Empty;
        public string UnlimitedMonthly { get; set; } = string.Empty;
    }
}
