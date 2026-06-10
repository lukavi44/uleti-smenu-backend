using Core.Models.Entities;

namespace Core.Repositories
{
    public interface IRestaurantLocationRepository : IRepository<RestaurantLocation>
    {
        Task<List<RestaurantLocation>> GetByEmployerIdAsync(Guid employerId);
        Task<RestaurantLocation?> GetByIdAsync(Guid locationId);
        Task<List<string>> GetDistinctCitiesAsync();
        Task<List<Guid>> GetEmployerIdsByCityAsync(string city);
    }
}
