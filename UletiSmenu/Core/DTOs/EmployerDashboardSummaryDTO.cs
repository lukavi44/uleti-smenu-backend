namespace Core.DTOs
{
    public class EmployerDashboardSummaryDTO
    {
        public int ActiveJobPostsCount { get; set; }
        public int PendingApplicantsCount { get; set; }
        public Dictionary<Guid, int> ActivePostsByLocationId { get; set; } = new();
    }
}
