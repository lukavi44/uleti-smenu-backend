namespace Core.Billing
{
    public static class BillingConstants
    {
        public static readonly Guid TrialPlanId = Guid.Parse("11111111-1111-1111-1111-111111111101");
        public static readonly Guid BasicSubscriptionPlanId = Guid.Parse("11111111-1111-1111-1111-111111111102");
        public static readonly Guid UnlimitedSubscriptionPlanId = Guid.Parse("11111111-1111-1111-1111-111111111103");

        public const int DefaultRegistrationFreeCredits = 5;
        public const decimal DefaultJobPostPriceRsd = 200m;
        public const decimal DefaultBasicMonthlyPriceRsd = 1990m;
        public const decimal DefaultUnlimitedMonthlyPriceRsd = 2990m;
        public const int DefaultBasicMaxActivePosts = 10;
        public const int MonthlyDurationDays = 30;
    }
}
