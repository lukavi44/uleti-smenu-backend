namespace Core.Billing
{
    public static class BillingConstants
    {
        public static readonly Guid TrialPlanId = Guid.Parse("11111111-1111-1111-1111-111111111101");
        public static readonly Guid BasicCreditPackPlanId = Guid.Parse("11111111-1111-1111-1111-111111111102");
        public static readonly Guid ProMonthlyPlanId = Guid.Parse("11111111-1111-1111-1111-111111111103");

        public const int TrialDurationDays = 90;
        public const decimal BasicCreditPackPriceEur = 29m;
        public const int BasicCreditPackCredits = 10;
        public const decimal ProMonthlyPriceEur = 49m;
        public const int ProMonthlyDurationDays = 30;
    }
}
