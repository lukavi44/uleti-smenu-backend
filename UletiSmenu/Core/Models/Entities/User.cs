using Core.Models.Enums;
using Core.Models.ValueObjects;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;

namespace Core.Models.Entities
{
    public class User : IdentityUser<Guid>
    {
        public string FirstName { get; }
        public string LastName { get; }
        public PhoneNumber PhoneNumber { get; private set; }
        public string ProfilePhoto { get; }
        public UserRolesEnum Role { get; }
        public Guid? CompanyId { get; }
        public Guid? JobPostId { get; }
        public Guid? SubscriptionId { get; }
        public DateTime? SubscriptionStart { get; }
        public DateTime? SubscriptionStop {  get; }

        private User(Guid id, string firstName, string lastName, string email, string username, PhoneNumber phoneNumber,
            Guid jobPostId, Guid subscriptionId, UserRolesEnum role, DateTime subscriptionStart, DateTime subscriptionStop)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            UserName = username;
            PhoneNumber = phoneNumber;
            JobPostId = jobPostId;
            SubscriptionId = subscriptionId;
            Role = role;
        }

        public User()
        {
        }

        public static Result<User> Create(Guid id, string firstName, string lastName, string email, string username, PhoneNumber phoneNumber,
            Guid jobPostId, Guid subscriptionId, UserRolesEnum role, DateTime subscriptionStart, DateTime subscriptionStop)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                return Result.Failure<User>("First name cannot be empty.");

            if (string.IsNullOrWhiteSpace(lastName))
                return Result.Failure<User>("Last name cannot be empty.");

            if (string.IsNullOrWhiteSpace(email))
                return Result.Failure<User>("Email cannot be empty.");

            if (string.IsNullOrWhiteSpace(username))
                return Result.Failure<User>("Username cannot be empty.");

            if (string.IsNullOrWhiteSpace(phoneNumber.Value))
                return Result.Failure<User>("Phone number cannot be empty.");

            if (role == default)
                return Result.Failure<User>("Role cannot be empty.");

            if (subscriptionStop > subscriptionStart)
                return Result.Failure<User>("Stop datum of subscription cannot be bigger than start datum.");

            if (subscriptionStart < DateTime.Now)
                return Result.Failure<User>("Start datum of subscription cannot be in the past.");

            var user = new User(id, firstName, lastName, email, username, phoneNumber, jobPostId, subscriptionId, role, subscriptionStart, subscriptionStop);
            return Result.Success(user);
        }
    }
}
