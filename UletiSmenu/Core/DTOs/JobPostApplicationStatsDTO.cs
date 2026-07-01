namespace Core.DTOs
{
    public class JobPostApplicationStatsDTO
    {
        public int TotalApplications { get; set; }
        public int Accepted { get; set; }
        public int Pending { get; set; }
        public int Denied { get; set; }
    }
}
