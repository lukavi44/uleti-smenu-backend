using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence.Database.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly UserManager<User> _userManager;
        public UserRepository(ApplicationDbContext context, UserManager<User> userManager) : base(context) 
        {
            _userManager = userManager;
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRolesEnum role)
        {
            var roleName = role.ToString();
            return await _userManager.GetUsersInRoleAsync(roleName);
        }
    }
}
