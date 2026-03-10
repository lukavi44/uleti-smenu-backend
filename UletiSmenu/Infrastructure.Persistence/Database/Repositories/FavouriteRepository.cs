using Core.Models;
using Core.Models.Entities;
using Core.Repositories;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database.Repositories
{
    internal class FavouriteRepository : IFavouriteRepository
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public FavouriteRepository(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        public async Task<Result> AddAsync(Favourite favourite)
        {
            await _applicationDbContext.Favourites.AddAsync(favourite);
            var result = await _applicationDbContext.SaveChangesAsync();

            if (result > 0)
            {
                return Result.Success("Favourite added successfully");
            }
            else
            {
                return Result.Failure("Failed to add favourite entry");
            }
        }

        public async Task<Maybe<Favourite>> GetByIdAsync(Guid employeeId, Guid employerId)
        {
            var favourite = await _applicationDbContext.Favourites
                .FirstOrDefaultAsync(f => f.EmployeeId == employeeId && f.EmployerId == employerId);

            return favourite == null ? Maybe<Favourite>.None : Maybe<Favourite>.From(favourite);
        }

        public async Task<Result> RemoveAsync(Favourite favourite)
        {
            var favouriteToDelete = await GetByIdAsync(favourite.EmployeeId, favourite.EmployerId);
            if (favouriteToDelete.HasNoValue)
            {
                return Result.Failure("Favourite entry not found");
            }

            _applicationDbContext.Favourites.Remove(favouriteToDelete.Value);
            var result = await _applicationDbContext.SaveChangesAsync();

            if (result == 0)
            {
                return Result.Failure("Failed to remove favourite entry");
            }
            else
            {
                return Result.Success("Favourite entry removed");
            }
        }

        public async Task<List<Guid>> GetEmployerIdsFavouritedByEmployeeAsync(Guid employeeId)
        {
            return await _applicationDbContext.Favourites
                .Where(f => f.EmployeeId == employeeId)
                .Select(f => f.EmployerId)
                .ToListAsync();
        }

        public async Task<List<Guid>> GetEmployeeIdsByEmployerIdAsync(Guid employerId)
        {
            return await _applicationDbContext.Favourites
                .Where(f => f.EmployerId == employerId)
                .Select(f => f.EmployeeId)
                .ToListAsync();
        }

        public async Task<List<string>> GetFollowerEmailsByEmployerIdAsync(Guid employerId)
        {
            return await (from favourite in _applicationDbContext.Favourites
                          join employee in _applicationDbContext.Users.OfType<Employee>()
                              on favourite.EmployeeId equals employee.Id
                          where favourite.EmployerId == employerId && employee.Email != null
                          select employee.Email!)
                .ToListAsync();
        }

    }
}
