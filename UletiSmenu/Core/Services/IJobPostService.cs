using Core.Models.Entities;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IJobPostService
    {
        Task<Result> CreateJobPostAsync(JobPost jobPost);
        Task<IEnumerable<JobPost>> GetAllJobPostsAsync();
    }
}
