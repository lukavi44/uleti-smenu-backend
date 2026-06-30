namespace Core.DTOs.Admin
{
    public class AdminEmployerDetailDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public string PIB { get; set; } = string.Empty;
        public string MB { get; set; } = string.Empty;
        public string StreetName { get; set; } = string.Empty;
        public string StreetNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public bool IsVerifiedEmployer { get; set; }
        public DateTime? VerifiedAtUtc { get; set; }
        public string? VerifiedByLabel { get; set; }
        public string BillingStatus { get; set; } = string.Empty;
        public string? SubscriptionPlanName { get; set; }
        public DateTime? SubscriptionStop { get; set; }
        public decimal WalletBalance { get; set; }
        public int ActiveJobPostsCount { get; set; }
        public int TotalJobPostsCount { get; set; }
        public int CompletedShiftsCount { get; set; }
        public int AcceptedCandidatesAllTime { get; set; }
        public double? AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime? CreatedAtUtc { get; set; }
    }
}
