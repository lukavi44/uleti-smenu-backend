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
                .Where(x => x.EmployerId == employerId)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<RestaurantLocation?> GetByIdAsync(Guid locationId)
        {
            return await _context.RestaurantLocations
                .FirstOrDefaultAsync(x => x.Id == locationId);
        }
    }
}
