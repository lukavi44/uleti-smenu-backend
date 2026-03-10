
using Core.Models;
using CSharpFunctionalExtensions;

namespace Core.Repositories
{
    public interface IFavouriteRepository
    {
        Task<Maybe<Favourite>> GetByIdAsync(Guid employeeId, Guid employerId);
        Task<Result> AddAsync(Favourite favourite);
        //Task<Result> RemoveAsync(Guid employeeId, Guid employerId);
        Task<Result> RemoveAsync(Favourite favourite);
        Task<List<Guid>> GetEmployerIdsFavouritedByEmployeeAsync(Guid employeeId);
        Task<List<Guid>> GetEmployeeIdsByEmployerIdAsync(Guid employerId);
        Task<List<string>> GetFollowerEmailsByEmployerIdAsync(Guid employerId);

    }
}