using Core.Models.Entities;
using Core.Models.Enums;

namespace Core.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRolesEnum role);
        Task<IEnumerable<Employer>> GetEmployerByCity(string city);
        Task<IEnumerable<Employer>> GetAllEmployers();
    }
}
