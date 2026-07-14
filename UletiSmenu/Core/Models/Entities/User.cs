using Core.Models.ValueObjects;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;

namespace Core.Models.Entities
{
    public class User : IdentityUser<Guid>
    {
        public string? ProfilePhoto { get; private set; }

        protected User(Guid id, string email, string username, string? phoneNumber, string? profilePhoto = null)
        {
            Id = id;
            Email = email;
            UserName = email;
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
            ProfilePhoto = profilePhoto ?? string.Empty;
        }

        public User()
        {
        }

        public static Result<User> Create(Guid id, string email, string username, string? phoneNumber, string? profilePhoto = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Result.Failure<User>("Email cannot be empty.");

            if (string.IsNullOrWhiteSpace(username))
                return Result.Failure<User>("Username cannot be empty.");

            var user = new User(id, email, username, phoneNumber, profilePhoto);
            return Result.Success(user);
        }

        public Result UpdateProfilePhoto(string photoUrl)
        {
            if (string.IsNullOrWhiteSpace(photoUrl))
            {
                return Result.Failure("Profile photo URL cannot be empty.");
            }

            ProfilePhoto = photoUrl;
            return Result.Success();
        }
    }
}
