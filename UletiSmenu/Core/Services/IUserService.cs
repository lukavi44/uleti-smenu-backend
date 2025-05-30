﻿using Core.Models.Entities;
using CSharpFunctionalExtensions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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

        // Employee
        Task<Result> RegisterEmployeeAsync(Employee employee, string password);
        Task<Result> UpdateEmployeeAsync(Guid employeeId, Employee employee);

        // Employer
        Task<IEnumerable<Employer>> GetAllEmployersAsync();
        Task<Employer> GetEmployerByCityAsync(string city);
        Task<Employer> GetEmployerByNameAsync(string name);

        Task<Result> UpdateEmployerAsync(Guid employerId, Employer employer);
        Task<Result> RegisterEmployerAsync(Employer employer, string password);
    }
}
