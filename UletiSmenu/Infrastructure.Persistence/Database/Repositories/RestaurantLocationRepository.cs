using Core.Models.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database.Repositories
{
    public class RestaurantLocationRepository : Repository<RestaurantLocation>, IRestaurantLocationRepository
    {
        public RestaurantLocationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<RestaurantLocation>> GetByEmployerIdAsync(Guid employerId)
        {
            return await _context.RestaurantLocations
                .Include(x => x.GeographyCity)
                .Where(x => x.EmployerId == employerId)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<RestaurantLocation?> GetByIdAsync(Guid locationId)
        {
            return await _context.RestaurantLocations
                .FirstOrDefaultAsync(x => x.Id == locationId);
        }

        public async Task<List<string>> GetDistinctCitiesAsync()
        {
            return await _context.RestaurantLocations
                .Select(location => location.City)
                .Where(city => !string.IsNullOrWhiteSpace(city))
                .Distinct()
                .OrderBy(city => city)
                .ToListAsync();
        }

        public async Task<List<Guid>> GetEmployerIdsByCityAsync(string city)
        {
            var normalizedCity = city.Trim().ToLower();
            return await _context.RestaurantLocations
                .Where(location => location.City.ToLower() == normalizedCity)
                .Select(location => location.EmployerId)
                .Distinct()
                .ToListAsync();
        }
    }
}
