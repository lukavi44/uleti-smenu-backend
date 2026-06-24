using Core.DTOs;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Services;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Services
{
    public class PlatformStatsService : IPlatformStatsService
    {
        private readonly ApplicationDbContext _context;

        public PlatformStatsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PlatformStatsDTO> GetPublicStatsAsync()
        {
            var employerCount = await _context.Users.OfType<Employer>().CountAsync();
            var employeeCount = await _context.Users.OfType<Employee>().CountAsync();
            var matchedCount = await _context.Applications
                .CountAsync(application => application.Status == ApplicationStatusEnum.Accepted);

            return new PlatformStatsDTO
            {
                MatchedCount = matchedCount,
                EmployerCount = employerCount,
                EmployeeCount = employeeCount
            };
        }
    }
}
