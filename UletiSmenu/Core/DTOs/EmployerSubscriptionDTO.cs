namespace Core.DTOs
{
    public class EmployerSubscriptionDTO
    {
        public string Status { get; set; } = "None";
        public string PlanTitle { get; set; } = string.Empty;
        public DateTime? SubscriptionStart { get; set; }
        public DateTime? SubscriptionStop { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsActive { get; set; }
    }
}
