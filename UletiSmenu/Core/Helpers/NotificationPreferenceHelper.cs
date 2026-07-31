using Core.DTOs;
using Core.Models.Entities;

namespace Core.Helpers
{
    public static class NotificationPreferenceHelper
    {
        public const string NewFavouriteRestaurantJobPostType = "NewFavouriteRestaurantJobPost";
        public const string ApplicationAcceptedType = "ApplicationAccepted";
        public const string ApplicationDeclinedType = "ApplicationDeclined";
        public const string ApplicationReceivedType = "ApplicationReceived";
        public const string ReviewReminderType = "ReviewReminder";

        public static bool IsInAppEnabled(User user, string notificationType)
        {
            if (notificationType.StartsWith(ApplicationReceivedType, StringComparison.Ordinal))
                return user.NotifyInAppApplicationReceived;

            return notificationType switch
            {
                NewFavouriteRestaurantJobPostType => user.NotifyInAppFavouriteJobPost,
                ApplicationAcceptedType => user.NotifyInAppApplicationAccepted,
                ApplicationDeclinedType => user.NotifyInAppApplicationDeclined,
                ReviewReminderType => user.NotifyInAppReviewReminder,
                _ => true
            };
        }

        public static NotificationPreferencesDTO ToDto(User user) =>
            new()
            {
                EmailFavouriteJobPost = user.NotifyEmailFavouriteJobPost,
                InAppFavouriteJobPost = user.NotifyInAppFavouriteJobPost,
                InAppApplicationAccepted = user.NotifyInAppApplicationAccepted,
                InAppApplicationDeclined = user.NotifyInAppApplicationDeclined,
                InAppApplicationReceived = user.NotifyInAppApplicationReceived,
                EmailApplicationReceived = user.NotifyEmailApplicationReceived,
                InAppReviewReminder = user.NotifyInAppReviewReminder
            };
    }
}
