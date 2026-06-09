using Core.Models.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database.Repositories
{
    public class WorkExperienceRepository : IWorkExperienceRepository
    {
        private readonly ApplicationDbContext _context;

        public WorkExperienceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<WorkExperience>> GetByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.WorkExperiences
                .Where(experience => experience.EmployeeId == employeeId)
                .OrderByDescending(experience => experience.StartDate)
                .ToListAsync();
        }

        public async Task<WorkExperience?> GetByIdAsync(Guid id)
        {
            return await _context.WorkExperiences.FirstOrDefaultAsync(experience => experience.Id == id);
        }

        public async Task AddAsync(WorkExperience workExperience)
        {
            await _context.WorkExperiences.AddAsync(workExperience);
        }

        public void Remove(WorkExperience workExperience)
        {
            _context.WorkExperiences.Remove(workExperience);
        }
    }
}
