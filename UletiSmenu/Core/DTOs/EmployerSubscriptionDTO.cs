namespace Core.DTOs
{
    public class EmployerSubscriptionDTO
    {
        public string Status { get; set; } = "None";
        public string PlanTitle { get; set; } = string.Empty;
        public string PlanKind { get; set; } = string.Empty;
        public DateTime? SubscriptionStart { get; set; }
        public DateTime? SubscriptionStop { get; set; }
        public DateTime? GracePeriodEndsAtUtc { get; set; }
        public int DaysRemaining { get; set; }
        public int FreePostingCredits { get; set; }
        public int PostCredits { get; set; }
        public decimal WalletBalance { get; set; }
        public string Currency { get; set; } = "RSD";
        public decimal JobPostPrice { get; set; }
        public int ActiveJobPostsCount { get; set; }
        public int MaxActivePosts { get; set; }
        public string? NextPostingChargeSource { get; set; }
        public bool IsActive { get; set; }
        public bool CanPost { get; set; }
        public bool NeedsAttention { get; set; }
        public bool CanManageBilling { get; set; }
    }
}
