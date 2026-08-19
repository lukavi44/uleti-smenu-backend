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

        public async Task<List<Employer>> GetEmployersLimitedAsync(int limit)
        {
            return await _context.Users
                .OfType<Employer>()
                .OrderBy(e => e.Name)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<(List<Employer> Items, int TotalCount)> GetEmployerDirectoryPagedAsync(
            string? city,
            string? search,
            int page,
            int pageSize)
        {
            var query = _context.Users
                .OfType<Employer>()
                .Where(employer => employer.DeletedAtUtc == null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLower();
                query = query.Where(employer => employer.Name.ToLower().Contains(normalizedSearch));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                var normalizedCity = city.Trim().ToLower();
                query = query.Where(employer =>
                    _context.RestaurantLocations.Any(location =>
                        location.EmployerId == employer.Id
                        && location.City.ToLower() == normalizedCity)
                    || employer.Address.City.Name.ToLower() == normalizedCity);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Include(employer => employer.Address)
                .Include(employer => employer.GeographyCity)
                .OrderBy(employer => employer.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
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
