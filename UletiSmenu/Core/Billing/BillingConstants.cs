namespace Core.Billing
{
    public static class BillingConstants
    {
        public static readonly Guid TrialPlanId = Guid.Parse("11111111-1111-1111-1111-111111111101");
        public static readonly Guid MonthlyStarterPlanId = Guid.Parse("11111111-1111-1111-1111-111111111102");
        public static readonly Guid YearlyStarterPlanId = Guid.Parse("11111111-1111-1111-1111-111111111103");

        public const int TrialDurationDays = 90;
        public const decimal MonthlyStarterPriceRsd = 4990m;
        public const decimal YearlyStarterPriceRsd = 49900m;
    }
}
