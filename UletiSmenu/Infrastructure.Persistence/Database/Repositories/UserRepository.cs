using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly UserManager<User> _userManager;
        public UserRepository(ApplicationDbContext context, UserManager<User> userManager) : base(context) 
        {
            _userManager = userManager;
        }

        public async Task<IEnumerable<Employer>> GetAllEmployers()
        {
            return await _context.Users
                .OfType<Employer>()
                .Include(e => e.Address)
                .ToListAsync();
        }

        public async Task<IEnumerable<Employer>> GetEmployerByCity(string city)
        {
            return await _context.Users.OfType<Employer>()
                .Where(c => c.Address.City.Name == city).ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRolesEnum role)
        {
            var roleName = role.ToString();
            return await _userManager.GetUsersInRoleAsync(roleName);
        }
    }
}
