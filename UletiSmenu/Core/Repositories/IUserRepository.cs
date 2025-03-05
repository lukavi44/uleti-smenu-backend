using Core.Models.Entities;
using Core.Models.Enums;

namespace Core.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRolesEnum role);
    }
}
