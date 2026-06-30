using Core.Models.Entities;

namespace Core.Repositories
{
    public interface IWorkExperienceRepository
    {
        Task<List<WorkExperience>> GetByEmployeeIdAsync(Guid employeeId);
        Task<int> CountByEmployeeIdAsync(Guid employeeId);
        Task<DateTime?> GetEarliestStartDateAsync(Guid employeeId);
        Task<List<WorkExperience>> GetByEmployeeIdPagedAsync(Guid employeeId, int page, int pageSize);
        Task<WorkExperience?> GetByIdAsync(Guid id);
        Task AddAsync(WorkExperience workExperience);
        void Remove(WorkExperience workExperience);
    }
}
