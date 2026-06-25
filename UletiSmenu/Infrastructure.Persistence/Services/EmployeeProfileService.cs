using Core.DTOs;
using Core.Helpers;
using Core.Models.Entities;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence.Services
{
    public class EmployeeProfileService : IEmployeeProfileService
    {
        private readonly IWorkExperienceRepository _workExperienceRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;
        private readonly UserManager<User> _userManager;

        public EmployeeProfileService(
            IWorkExperienceRepository workExperienceRepository,
            IApplicationRepository applicationRepository,
            IReviewRepository reviewRepository,
            IApplicationUnitOfWork applicationUnitOfWork,
            UserManager<User> userManager)
        {
            _workExperienceRepository = workExperienceRepository;
            _applicationRepository = applicationRepository;
            _reviewRepository = reviewRepository;
            _applicationUnitOfWork = applicationUnitOfWork;
            _userManager = userManager;
        }

        public async Task<Result<List<WorkExperienceDTO>>> GetMyWorkExperiencesAsync(Guid employeeId)
        {
            var experiences = await _workExperienceRepository.GetByEmployeeIdAsync(employeeId);
            return Result.Success(experiences.Select(MapWorkExperience).ToList());
        }

        public async Task<Result<WorkExperienceDTO>> CreateWorkExperienceAsync(
            Guid employeeId,
            string companyName,
            string position,
            DateTime startDate,
            DateTime? endDate,
            string? description)
        {
            var createResult = WorkExperience.Create(employeeId, companyName, position, startDate, endDate, description);
            if (createResult.IsFailure)
                return Result.Failure<WorkExperienceDTO>(createResult.Error);

            await _workExperienceRepository.AddAsync(createResult.Value);
            await _applicationUnitOfWork.SaveChangesAsync();

            return Result.Success(MapWorkExperience(createResult.Value));
        }

        public async Task<Result<WorkExperienceDTO>> UpdateWorkExperienceAsync(
            Guid employeeId,
            Guid experienceId,
            string companyName,
            string position,
            DateTime startDate,
            DateTime? endDate,
            string? description)
        {
            var experience = await _workExperienceRepository.GetByIdAsync(experienceId);
            if (experience == null)
                return Result.Failure<WorkExperienceDTO>("Work experience not found.");

            if (experience.EmployeeId != employeeId)
                return Result.Failure<WorkExperienceDTO>("You do not have access to this work experience.");

            var updateResult = experience.Update(companyName, position, startDate, endDate, description);
            if (updateResult.IsFailure)
                return Result.Failure<WorkExperienceDTO>(updateResult.Error);

            await _applicationUnitOfWork.SaveChangesAsync();
            return Result.Success(MapWorkExperience(experience));
        }

        public async Task<Result> DeleteWorkExperienceAsync(Guid employeeId, Guid experienceId)
        {
            var experience = await _workExperienceRepository.GetByIdAsync(experienceId);
            if (experience == null)
                return Result.Failure("Work experience not found.");

            if (experience.EmployeeId != employeeId)
                return Result.Failure("You do not have access to this work experience.");

            _workExperienceRepository.Remove(experience);
            await _applicationUnitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result<List<EmployeePlatformShiftDTO>>> GetMyPlatformShiftsAsync(Guid employeeId)
        {
            return Result.Success(await BuildPlatformShiftsAsync(employeeId));
        }

        public async Task<Result<EmployeePublicProfileDTO>> GetEmployeeProfileForEmployerAsync(
            Guid employerId,
            Guid employeeId,
            bool includeContactInfo = false)
        {
            if (!await _applicationRepository.EmployerCanViewEmployeeAsync(employerId, employeeId))
                return Result.Failure<EmployeePublicProfileDTO>("You do not have access to this employee profile.");

            var employee = await _userManager.FindByIdAsync(employeeId.ToString()) as Employee;
            if (employee == null)
                return Result.Failure<EmployeePublicProfileDTO>("Employee not found.");

            var workExperiences = await _workExperienceRepository.GetByEmployeeIdAsync(employeeId);
            var platformShifts = await BuildPlatformShiftsAsync(employeeId);
            var reviewSummary = await _reviewRepository.GetEmployeeReviewSummaryAsync(employeeId);
            var reviews = await _reviewRepository.GetReviewsForEmployeeAsync(employeeId);

            var profile = new EmployeePublicProfileDTO
            {
                EmployeeId = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email ?? string.Empty,
                PhoneNumber = employee.PhoneNumber ?? string.Empty,
                ProfilePhoto = employee.ProfilePhoto,
                WorkExperiences = workExperiences.Select(MapWorkExperience).ToList(),
                PlatformShifts = platformShifts,
                ReviewSummary = reviewSummary,
                Reviews = reviews
            };

            if (!includeContactInfo)
                CandidateContactPrivacy.RedactPublicProfileContactInfo(profile);

            return Result.Success(profile);
        }

        private async Task<List<EmployeePlatformShiftDTO>> BuildPlatformShiftsAsync(Guid employeeId)
        {
            var utcNow = DateTime.UtcNow;
            var acceptedApplications = await _applicationRepository.GetAcceptedApplicationsWithDetailsAsync(employeeId);

            return acceptedApplications
                .Where(row => row.JobPost.IsArchived(utcNow))
                .Select(row => new EmployeePlatformShiftDTO
                {
                    ApplicationId = row.Application.Id,
                    JobPostId = row.JobPost.Id,
                    JobPostTitle = row.JobPost.Title,
                    Position = row.JobPost.Position,
                    EmployerName = row.Employer.Name,
                    RestaurantLocationName = row.Location?.Name,
                    RestaurantLocationCity = row.Location?.City,
                    StartingDate = row.JobPost.StartingDate,
                    Salary = row.JobPost.Salary,
                    CompletedAtUtc = row.JobPost.StartingDate.AddHours(1)
                })
                .ToList();
        }

        private static WorkExperienceDTO MapWorkExperience(WorkExperience experience)
        {
            return new WorkExperienceDTO
            {
                Id = experience.Id,
                CompanyName = experience.CompanyName,
                Position = experience.Position,
                StartDate = experience.StartDate,
                EndDate = experience.EndDate,
                Description = experience.Description
            };
        }
    }
}
