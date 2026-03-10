using Core.Models.Entities;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IJobPostService
    {
        Task<Result> CreateJobPostAsync(JobPost jobPost);
        Task<Result> UpdateJobPostAsync(
            Guid employerId,
            Guid jobPostId,
            string title,
            string description,
            string position,
            string status,
            int salary,
            DateTime startingDate,
            DateTime? visibleUntil,
            Guid restaurantLocationId);
        Task<IEnumerable<JobPost>> GetVisibleJobPostsAsync();
        Task<IEnumerable<JobPost>> GetMyJobPostsAsync(Guid employerId);
    }
}
