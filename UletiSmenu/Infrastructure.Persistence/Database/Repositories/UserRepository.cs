using Core.Models.Entities;
using Core.Models.Enums;
using Core.Models.ValueObjects;
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

        public async Task<IEnumerable<Employer>> GetAllEmployersAsync()
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

        public Task<CSharpFunctionalExtensions.Result<Employer>> GetEmployerByIdAsync(Guid id)
        {
            return GetEmployerByIdResultAsync(id);
        }

        private async Task<CSharpFunctionalExtensions.Result<Employer>> GetEmployerByIdResultAsync(Guid id)
        {
            var employer = await _context.Users
                .OfType<Employer>()
                .Include(e => e.GeographyCity)
                .FirstOrDefaultAsync(e => e.Id == id);

            return employer == null
                ? CSharpFunctionalExtensions.Result.Failure<Employer>("Employer not found.")
                : CSharpFunctionalExtensions.Result.Success(employer);
        }

        public async Task<Employer?> FindEmployerByPublicSlugAsync(string publicSlug)
        {
            var normalizedSlug = publicSlug.Trim().ToLowerInvariant();
            return await _context.Users
                .OfType<Employer>()
                .Include(e => e.GeographyCity)
                .FirstOrDefaultAsync(e => e.PublicSlug == normalizedSlug);
        }

        public async Task<bool> PublicSlugExistsAsync(string publicSlug, Guid excludeEmployerId)
        {
            var normalizedSlug = publicSlug.Trim().ToLowerInvariant();
            return await _context.Users
                .OfType<Employer>()
                .AnyAsync(e => e.PublicSlug == normalizedSlug && e.Id != excludeEmployerId);
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRolesEnum role)
        {
            var roleName = role.ToString();
            return await _userManager.GetUsersInRoleAsync(roleName);
        }
        //public async Task<Employee?> GetEmployeeWithFavouritesAsync(Guid employeeId)
        //{
        //    return await _context.Users
        //        .OfType<Employee>()
        //        .Include(e => e.FavouriteEmployers)
        //        .FirstOrDefaultAsync(e => e.Id == employeeId);
        //}

        public async Task<T> GetByIdAsync<T>(Guid id) where T : class
        {
            if (typeof(T) == typeof(Employer))
            {
                var employer = await _context.Users
                    .OfType<Employer>()
                    .FirstOrDefaultAsync(e => e.Id == id);

                return employer == null ? default! : (T)(object)employer;
            }

            return await _context.Set<T>().FindAsync(id);
        }

        public Task<Employee?> GetEmployeeWithFavouritesAsync(Guid employeeId)
        {
            throw new NotImplementedException();
        }

        public async Task<Employer?> FindEmployerByStripeSubscriptionIdAsync(string stripeSubscriptionId)
        {
            return await _context.Users
                .OfType<Employer>()
                .FirstOrDefaultAsync(e => e.StripeSubscriptionId == stripeSubscriptionId);
        }
    }
}
