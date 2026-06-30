namespace Core.DTOs
{
    public class EmployeePublicProfileDTO
    {
        public Guid EmployeeId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public DateTime? MemberSinceUtc { get; set; }
        public int? Age { get; set; }
        public double? TotalExperienceYears { get; set; }
        public ReviewSummaryDTO ReviewSummary { get; set; } = new();
        public int WorkExperienceCount { get; set; }
        public int PlatformShiftCount { get; set; }
    }
}
