using Core.Models.Entities;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User> CreateUserAsync(User user);
        Task<bool> UpdateUserAsync(Guid id, User updatedUser);
        Task<bool> DeleteUserAsync(Guid id);

        // ✅ Authentication Methods
        Task<Result> RegisterEmployeeAsync(Employee employee, string password);
        Task<Result> RegisterEmployerAsync(Employer employer, string password);
        Task<Result> LoginUserAsync(string email, string password);
        Task<Result> LogoutUserAsync(string email, string password);
        Task<IEnumerable<User>> GetUsersByRoleAsync(string userRole);
        Task<bool> ConfirmEmailAsync(Guid userId, string token);
    }
}
