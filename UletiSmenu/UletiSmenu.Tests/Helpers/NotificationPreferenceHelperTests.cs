using Core.DTOs;
using Core.Helpers;
using Core.Models.Entities;
using Xunit;

namespace UletiSmenu.Tests.Helpers
{
    public class NotificationPreferenceHelperTests
    {
        [Theory]
        [InlineData(NotificationPreferenceHelper.NewFavouriteRestaurantJobPostType, true, true)]
        [InlineData(NotificationPreferenceHelper.NewFavouriteRestaurantJobPostType, false, false)]
        [InlineData(NotificationPreferenceHelper.ApplicationAcceptedType, true, true)]
        [InlineData(NotificationPreferenceHelper.ApplicationAcceptedType, false, false)]
        [InlineData(NotificationPreferenceHelper.ApplicationDeclinedType, true, true)]
        [InlineData(NotificationPreferenceHelper.ApplicationDeclinedType, false, false)]
        [InlineData($"{NotificationPreferenceHelper.ApplicationReceivedType}:123", true, true)]
        [InlineData($"{NotificationPreferenceHelper.ApplicationReceivedType}:123", false, false)]
        [InlineData(NotificationPreferenceHelper.ReviewReminderType, true, true)]
        [InlineData(NotificationPreferenceHelper.ReviewReminderType, false, false)]
        public void IsInAppEnabled_RespectsUserPreferences(string notificationType, bool enabled, bool expected)
        {
            var user = CreateUserWithPreferences(
                inAppFavouriteJobPost: notificationType == NotificationPreferenceHelper.NewFavouriteRestaurantJobPostType ? enabled : true,
                inAppApplicationAccepted: notificationType == NotificationPreferenceHelper.ApplicationAcceptedType ? enabled : true,
                inAppApplicationDeclined: notificationType == NotificationPreferenceHelper.ApplicationDeclinedType ? enabled : true,
                inAppApplicationReceived: notificationType.StartsWith(NotificationPreferenceHelper.ApplicationReceivedType) ? enabled : true,
                inAppReviewReminder: notificationType == NotificationPreferenceHelper.ReviewReminderType ? enabled : true);

            Assert.Equal(expected, NotificationPreferenceHelper.IsInAppEnabled(user, notificationType));
        }

        [Fact]
        public void ToDto_MapsAllPreferenceFields()
        {
            var user = CreateUserWithPreferences(
                emailFavouriteJobPost: false,
                inAppFavouriteJobPost: true,
                inAppApplicationAccepted: false,
                inAppApplicationDeclined: true,
                inAppApplicationReceived: false,
                inAppReviewReminder: true);

            var dto = NotificationPreferenceHelper.ToDto(user);

            Assert.False(dto.EmailFavouriteJobPost);
            Assert.True(dto.InAppFavouriteJobPost);
            Assert.False(dto.InAppApplicationAccepted);
            Assert.True(dto.InAppApplicationDeclined);
            Assert.False(dto.InAppApplicationReceived);
            Assert.True(dto.InAppReviewReminder);
        }

        [Fact]
        public void ApplyNotificationPreferences_UpdatesOnlyProvidedFields()
        {
            var user = CreateUserWithPreferences(
                emailFavouriteJobPost: true,
                inAppFavouriteJobPost: true,
                inAppApplicationAccepted: true,
                inAppApplicationDeclined: true,
                inAppApplicationReceived: true,
                inAppReviewReminder: true);

            user.ApplyNotificationPreferences(new UpdateNotificationPreferencesDTO
            {
                EmailFavouriteJobPost = false,
                InAppReviewReminder = false
            });

            var dto = NotificationPreferenceHelper.ToDto(user);

            Assert.False(dto.EmailFavouriteJobPost);
            Assert.True(dto.InAppFavouriteJobPost);
            Assert.True(dto.InAppApplicationAccepted);
            Assert.True(dto.InAppApplicationDeclined);
            Assert.True(dto.InAppApplicationReceived);
            Assert.False(dto.InAppReviewReminder);
        }

        private static User CreateUserWithPreferences(
            bool emailFavouriteJobPost = true,
            bool inAppFavouriteJobPost = true,
            bool inAppApplicationAccepted = true,
            bool inAppApplicationDeclined = true,
            bool inAppApplicationReceived = true,
            bool inAppReviewReminder = true)
        {
            var user = User.Create(Guid.NewGuid(), "test@example.com", "test@example.com", null).Value;
            user.ApplyNotificationPreferences(new UpdateNotificationPreferencesDTO
            {
                EmailFavouriteJobPost = emailFavouriteJobPost,
                InAppFavouriteJobPost = inAppFavouriteJobPost,
                InAppApplicationAccepted = inAppApplicationAccepted,
                InAppApplicationDeclined = inAppApplicationDeclined,
                InAppApplicationReceived = inAppApplicationReceived,
                InAppReviewReminder = inAppReviewReminder
            });
            return user;
        }
    }
}
