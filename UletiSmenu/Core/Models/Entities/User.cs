using Core.Models.ValueObjects;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;

namespace Core.Models.Entities
{
    public class User : IdentityUser<Guid>
    {
        public string ProfilePhoto { get; set; }

        protected User(Guid id, string email, string username, string phoneNumber, string profilePhoto)
        {
            Id = id;
            Email = email;
            UserName = email;
            PhoneNumber = phoneNumber;
            ProfilePhoto = profilePhoto;
        }

        public User()
        {
        }

        public static Result<User> Create(Guid id, string email, string username, string phoneNumber, string profilePhoto)
        {

            if (string.IsNullOrWhiteSpace(email))
                return Result.Failure<User>("Email cannot be empty.");

            if (string.IsNullOrWhiteSpace(username))
                return Result.Failure<User>("Username cannot be empty.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return Result.Failure<User>("Phone number cannot be empty.");

            var user = new User(id, email, username, phoneNumber, profilePhoto);
            return Result.Success(user);
        }
    }
}
