using Core.Models.Entities;
using Core.Models.Enums;
using CSharpFunctionalExtensions;

namespace Core.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRolesEnum role);
        Task<IEnumerable<Employer>> GetEmployerByCity(string city);
        Task<IEnumerable<Employer>> GetAllEmployersAsync();
        Task<Result<Employer>> GetEmployerByIdAsync(Guid id);
        Task<Employee?> GetEmployeeWithFavouritesAsync(Guid employeeId);
        Task<T> GetByIdAsync<T>(Guid id) where T : class;
        Task<Employer?> FindEmployerByStripeSubscriptionIdAsync(string stripeSubscriptionId);
        Task<Employer?> FindEmployerByPublicSlugAsync(string publicSlug);
        Task<bool> PublicSlugExistsAsync(string publicSlug, Guid excludeEmployerId);
    }
}
