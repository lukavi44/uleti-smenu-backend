namespace Core.DTOs
{
    public class NotificationPreferencesDTO
    {
        public bool EmailFavouriteJobPost { get; set; }
        public bool InAppFavouriteJobPost { get; set; }
        public bool InAppApplicationAccepted { get; set; }
        public bool InAppApplicationDeclined { get; set; }
        public bool InAppApplicationReceived { get; set; }
        public bool InAppReviewReminder { get; set; }
    }

    public class UpdateNotificationPreferencesDTO
    {
        public bool? EmailFavouriteJobPost { get; set; }
        public bool? InAppFavouriteJobPost { get; set; }
        public bool? InAppApplicationAccepted { get; set; }
        public bool? InAppApplicationDeclined { get; set; }
        public bool? InAppApplicationReceived { get; set; }
        public bool? InAppReviewReminder { get; set; }
    }

    public sealed record JobAlertFollower(
        Guid EmployeeId,
        string? Email,
        bool EmailEnabled,
        bool InAppEnabled);
}
