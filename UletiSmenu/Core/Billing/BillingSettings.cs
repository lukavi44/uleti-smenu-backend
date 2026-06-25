namespace Core.Billing
{
    public class BillingSettings
    {
        public const string SectionName = "Billing";

        public int GracePeriodDays { get; set; } = 7;
        public string Currency { get; set; } = "RSD";
        public int RegistrationFreeCredits { get; set; } = BillingConstants.DefaultRegistrationFreeCredits;
        public decimal JobPostPrice { get; set; } = BillingConstants.DefaultJobPostPriceRsd;
        public decimal[] SuggestedTopUpAmounts { get; set; } = [500m, 1000m, 2000m];

        public PlanLimitSettings Basic { get; set; } = new() { MaxActivePosts = BillingConstants.DefaultBasicMaxActivePosts };
        public PlanLimitSettings Unlimited { get; set; } = new() { MaxActivePosts = -1 };

        public decimal BasicMonthlyPrice { get; set; } = BillingConstants.DefaultBasicMonthlyPriceRsd;
        public decimal UnlimitedMonthlyPrice { get; set; } = BillingConstants.DefaultUnlimitedMonthlyPriceRsd;
    }

    public class PlanLimitSettings
    {
        public int MaxActivePosts { get; set; }
    }
}
