using Core.Models.Entities;

namespace Core.Repositories
{
    public interface IWorkExperienceRepository
    {
        Task<List<WorkExperience>> GetByEmployeeIdAsync(Guid employeeId);
        Task<WorkExperience?> GetByIdAsync(Guid id);
        Task AddAsync(WorkExperience workExperience);
        void Remove(WorkExperience workExperience);
    }
}
