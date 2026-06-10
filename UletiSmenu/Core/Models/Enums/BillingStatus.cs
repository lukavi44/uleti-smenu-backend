namespace Core.Models.Enums
{
    /// <summary>
    /// Internal billing status — independent from Stripe raw statuses.
    /// </summary>
    public enum BillingStatus
    {
        Trialing = 0,
        Active = 1,
        PastDue = 2,
        Canceled = 3,
        Expired = 4,
        Incomplete = 5
    }
}
