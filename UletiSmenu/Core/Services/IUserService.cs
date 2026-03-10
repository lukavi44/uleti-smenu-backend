using Core.DTOs;
using Core.Models.Entities;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(Guid id);
        Task<bool> DeleteUserAsync(Guid id);
        //Task<Result> LoginUserAsync(string email, string password);
        Task<Result> LogoutUserAsync();
        Task<IEnumerable<User>> GetUsersByRoleAsync(string userRole);
        Task<bool> ConfirmEmailAsync(Guid userId, string token);
        Task<string?> GetUserRoleAsync(Guid userId);

        // Employee
        Task<Result> RegisterEmployeeAsync(Employee employee, string password);
        Task<Result> UpdateEmployeeAsync(Guid employeeId, Employee employee);
        Task<Result> ToggleFavouriteEmployerAsync(Guid employeId, Guid employerId);
        Task<IEnumerable<EmployerFavouriteStatusDTO>> GetAllEmployersWithFavouriteStatusAsync(Guid employeeId);
        Task<Result<RestaurantLocation>> CreateEmployerLocationAsync(Guid employerId, string name, string phoneNumber, string pib, string mb, string streetName, string streetNumber, string city, string postalCode, string country, string region);
        Task<IEnumerable<RestaurantLocation>> GetEmployerLocationsAsync(Guid employerId);

        // Employer
        Task<IEnumerable<Employer>> GetAllEmployersAsync();
        Task<Employer> GetEmployerByCityAsync(string city);
        Task<Employer> GetEmployerByNameAsync(string name);

        Task<Result> UpdateEmployerAsync(Guid employerId, Employer employer);
        Task<Result> RegisterEmployerAsync(Employer employer, string password);
    }
}
