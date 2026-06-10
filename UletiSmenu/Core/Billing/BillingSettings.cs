namespace Core.Billing
{
    public class BillingSettings
    {
        public const string SectionName = "Billing";

        public int GracePeriodDays { get; set; } = 7;
        public string Currency { get; set; } = "EUR";

        public PlanLimitSettings Trial { get; set; } = new();
        public PlanLimitSettings Basic { get; set; } = new();
        public PlanLimitSettings Pro { get; set; } = new();
    }

    public class PlanLimitSettings
    {
        public int MaxActivePosts { get; set; }
        public int CreditsPerPost { get; set; } = 1;
    }
}
