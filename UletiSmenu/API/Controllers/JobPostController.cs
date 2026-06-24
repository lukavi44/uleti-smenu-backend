using API.DTOs;
using AutoMapper;
using Core.DTOs;
using Core.Models.Entities;
using Core.Repositories;
using Core.Services;
using Infrastructure.Persistence.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class JobPostController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IJobPostService _jobPostService;
        private readonly IApplicationRepository _applicationRepository;
        private readonly UserManager<User> _userManager;

        public JobPostController(
            ApplicationDbContext context,
            IMapper mapper,
            IUserService userService,
            IJobPostService jobPostService,
            IApplicationRepository applicationRepository,
            UserManager<User> userManager)
        {
            _context = context;
            _mapper = mapper;
            _userService = userService;
            _jobPostService = jobPostService;
            _applicationRepository = applicationRepository;
            _userManager = userManager;
        }

        [Authorize(Roles = "Employer")]
        [HttpPost("createJobPost")]
        public async Task<IActionResult> CreateJobPost([FromBody] JobPostCreateDTO jobPostCreateDTO)
        {
            var employerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(employerIdClaim))
                return Unauthorized("Employer ID claim not found");

            var employerId = Guid.Parse(employerIdClaim);

            var jobPost = _mapper.Map<JobPost>(jobPostCreateDTO, opt =>
            {
                opt.Items["EmployerId"] = employerId;
            });

            var result = await _jobPostService.CreateJobPostAsync(jobPost);

            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { message = "Job post created successfully." });
        }

        [Authorize]
        [HttpPut("my/{jobPostId:guid}")]
        public async Task<IActionResult> UpdateMyJobPost(Guid jobPostId, [FromBody] JobPostCreateDTO jobPostCreateDTO)
        {
            var employerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(employerIdClaim, out var employerId))
                return Unauthorized("Invalid user claim.");

            var user = await _userService.GetUserByIdAsync(employerId);
            if (user is not Employer)
                return Forbid();

            var result = await _jobPostService.UpdateJobPostAsync(
                employerId,
                jobPostId,
                jobPostCreateDTO.Title,
                jobPostCreateDTO.Description,
                jobPostCreateDTO.Position,
                jobPostCreateDTO.Status,
                jobPostCreateDTO.Salary,
                jobPostCreateDTO.StartingDate,
                jobPostCreateDTO.VisibleUntil,
                jobPostCreateDTO.RestaurantLocationId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { message = "Job post updated successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllJobPosts([FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null)
        {
            var jobPosts = await _jobPostService.GetVisibleJobPostsAsync(sortBy, sortDirection);

            var jobPostDtos = _mapper.Map<List<JobPostDTO>>(jobPosts);

            return Ok(jobPostDtos);
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyJobPosts(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? position = null,
            [FromQuery] string? status = null,
            [FromQuery] string? lifecycle = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDirection = null,
            [FromQuery] bool? hasApplicants = null,
            [FromQuery] string? city = null,
            [FromQuery] Guid? restaurantLocationId = null,
            [FromQuery] int? minSalary = null,
            [FromQuery] int? maxSalary = null)
        {
            var employerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(employerIdClaim, out var employerId))
                return Unauthorized("Invalid user claim.");

            var user = await _userService.GetUserByIdAsync(employerId);
            if (user is not Employer)
                return Forbid();

            if (page.HasValue || pageSize.HasValue)
            {
                var pagedJobPosts = await _jobPostService.GetMyJobPostsPagedAsync(
                    employerId,
                    page ?? 1,
                    pageSize ?? 6,
                    position,
                    status,
                    lifecycle,
                    sortBy,
                    sortDirection,
                    hasApplicants,
                    city,
                    restaurantLocationId,
                    minSalary,
                    maxSalary);

                var jobPostDtos = _mapper.Map<List<JobPostDTO>>(pagedJobPosts.Items);
                await EnrichApplicantCountsAsync(jobPostDtos);

                return Ok(new PagedResultDTO<JobPostDTO>
                {
                    Items = jobPostDtos,
                    TotalCount = pagedJobPosts.TotalCount,
                    Page = pagedJobPosts.Page,
                    PageSize = pagedJobPosts.PageSize
                });
            }

            var jobPosts = await _jobPostService.GetMyJobPostsAsync(employerId);
            var allJobPostDtos = _mapper.Map<List<JobPostDTO>>(jobPosts);
            await EnrichApplicantCountsAsync(allJobPostDtos);

            return Ok(allJobPostDtos);
        }

        [Authorize]
        [HttpGet("my/dashboard-summary")]
        public async Task<IActionResult> GetMyDashboardSummary()
        {
            var employerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(employerIdClaim, out var employerId))
                return Unauthorized("Invalid user claim.");

            var user = await _userService.GetUserByIdAsync(employerId);
            if (user is not Employer)
                return Forbid();

            var summary = await _jobPostService.GetEmployerDashboardSummaryAsync(employerId);
            return Ok(summary);
        }

        private async Task EnrichApplicantCountsAsync(List<JobPostDTO> jobPostDtos)
        {
            if (jobPostDtos.Count == 0)
                return;

            var counts = await _applicationRepository.GetApplicantCountsByJobPostIdsAsync(
                jobPostDtos.Select(jobPost => jobPost.Id));

            foreach (var jobPostDto in jobPostDtos)
            {
                jobPostDto.ApplicantCount = counts.GetValueOrDefault(jobPostDto.Id);
            }
        }

        [Authorize]
        [HttpGet("my/positions")]
        public async Task<IActionResult> GetMyJobPostPositions()
        {
            var employerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(employerIdClaim, out var employerId))
                return Unauthorized("Invalid user claim.");

            var user = await _userService.GetUserByIdAsync(employerId);
            if (user is not Employer)
                return Forbid();

            var positions = await _jobPostService.GetMyJobPostPositionsAsync(employerId);
            return Ok(positions);
        }
    }
}