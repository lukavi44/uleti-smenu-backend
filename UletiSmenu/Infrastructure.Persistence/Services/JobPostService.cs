using Core.Models.Entities;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;

namespace Infrastructure.Persistence.Services
{
    public class JobPostService : IJobPostService
    {
        private readonly IJobPostRepository _jobPostRepository;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;
        private readonly IUserRepository _userRepository;

        public JobPostService(IJobPostRepository jobPostRepository, IApplicationUnitOfWork applicationUnitOfWork, IUserRepository userRepository) { 
            _jobPostRepository = jobPostRepository;
            _applicationUnitOfWork = applicationUnitOfWork;
            _userRepository = userRepository;
        }
        public async Task<Result> CreateJobPostAsync(JobPost jobPost)
        {
            await _applicationUnitOfWork.BeginTransactionAsync();

            try
            {
                await _jobPostRepository.AddAsync(jobPost);
                await _applicationUnitOfWork.SaveChangesAsync();
                await _applicationUnitOfWork.CommitTransactionAsync();

                return Result.Success("Job post created successfully.");

            }
            catch (Exception ex)
            {
                await _applicationUnitOfWork.RollbackTransactionAsync();
                return Result.Failure($"Job post creation failed: {ex.Message}");
            }
        }

        public async Task<IEnumerable<JobPost>> GetAllJobPostsAsync()
        {
            return await _jobPostRepository.GetAllAsync();
        }
    }
}
