namespace Core.DTOs.Admin
{
    public class AdminDashboardDTO
    {
        public int TotalCandidates { get; set; }
        public int TotalEmployers { get; set; }
        public int ActiveJobPosts { get; set; }
        public int ReportsCount { get; set; }
        public decimal WalletTopUpsThisMonth { get; set; }
        public int AcceptedCandidatesAllTime { get; set; }
        public int CompletedShiftsAllTime { get; set; }
        public List<AdminDashboardChartPointDTO> ApplicationsChart { get; set; } = new();
        public List<AdminRecentActivityDTO> RecentActivities { get; set; } = new();
    }

    public class AdminDashboardChartPointDTO
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class AdminRecentActivityDTO
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public Guid? RelatedEntityId { get; set; }
    }
}
