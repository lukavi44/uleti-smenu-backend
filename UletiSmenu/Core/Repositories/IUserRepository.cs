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
    }
}
