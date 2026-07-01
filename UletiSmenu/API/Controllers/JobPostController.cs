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
        public async Task<IActionResult> GetAllJobPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 6,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDirection = null,
            [FromQuery] string? city = null,
            [FromQuery] Guid? restaurantLocationId = null,
            [FromQuery] string? position = null,
            [FromQuery] List<string>? positions = null,
            [FromQuery] int? minSalary = null,
            [FromQuery] int? maxSalary = null,
            [FromQuery] DateTime? shiftDateFrom = null,
            [FromQuery] DateTime? shiftDateTo = null,
            [FromQuery] string? applicationFilter = null,
            [FromQuery] bool? favouritesOnly = null)
        {
            Guid? employeeId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    if (user is Employee)
                    {
                        employeeId = userId;
                    }
                }
            }

            var normalizedApplicationFilter = applicationFilter?.Trim().ToLowerInvariant();
            if (normalizedApplicationFilter is not ("applied" or "notapplied"))
            {
                normalizedApplicationFilter = null;
            }

            var pagedJobPosts = await _jobPostService.GetVisibleJobPostsPagedAsync(
                page,
                pageSize,
                sortBy,
                sortDirection,
                city,
                restaurantLocationId,
                position,
                positions,
                minSalary,
                maxSalary,
                shiftDateFrom,
                shiftDateTo,
                employeeId,
                normalizedApplicationFilter,
                favouritesOnly);

            var jobPostDtos = _mapper.Map<List<JobPostDTO>>(pagedJobPosts.Items);

            return Ok(new PagedResultDTO<JobPostDTO>
            {
                Items = jobPostDtos,
                TotalCount = pagedJobPosts.TotalCount,
                Page = pagedJobPosts.Page,
                PageSize = pagedJobPosts.PageSize
            });
        }

        [HttpGet("filter-options")]
        public async Task<IActionResult> GetVisibleJobPostFilterOptions([FromQuery] string? city = null)
        {
            var options = await _jobPostService.GetVisibleJobPostFilterOptionsAsync(city);
            return Ok(options);
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
                await EnrichApplicantDataAsync(jobPostDtos);

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
            await EnrichApplicantDataAsync(allJobPostDtos);

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

        private async Task EnrichApplicantDataAsync(List<JobPostDTO> jobPostDtos)
        {
            if (jobPostDtos.Count == 0)
                return;

            var jobPostIds = jobPostDtos.Select(jobPost => jobPost.Id).ToList();
            var counts = await _applicationRepository.GetApplicantCountsByJobPostIdsAsync(jobPostIds);
            var recentApplicants = await _applicationRepository.GetRecentApplicantsByJobPostIdsAsync(jobPostIds);

            foreach (var jobPostDto in jobPostDtos)
            {
                jobPostDto.ApplicantCount = counts.GetValueOrDefault(jobPostDto.Id);
                jobPostDto.RecentApplicants = recentApplicants.GetValueOrDefault(jobPostDto.Id) ?? new List<RecentApplicantPreviewDTO>();
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

        [Authorize(Roles = "Employer")]
        [HttpGet("my/{jobPostId:guid}/application-stats")]
        public async Task<IActionResult> GetMyJobPostApplicationStats(Guid jobPostId)
        {
            var employerId = GetEmployerId();
            if (employerId == null)
                return Unauthorized("Invalid user claim.");

            var result = await _jobPostService.GetMyJobPostApplicationStatsAsync(employerId.Value, jobPostId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [Authorize(Roles = "Employer")]
        [HttpPost("my/{jobPostId:guid}/duplicate")]
        public async Task<IActionResult> DuplicateMyJobPost(Guid jobPostId)
        {
            var employerId = GetEmployerId();
            if (employerId == null)
                return Unauthorized("Invalid user claim.");

            var result = await _jobPostService.DuplicateMyJobPostAsync(employerId.Value, jobPostId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(_mapper.Map<JobPostDTO>(result.Value));
        }

        [Authorize(Roles = "Employer")]
        [HttpPost("my/{jobPostId:guid}/archive")]
        public async Task<IActionResult> ArchiveMyJobPost(Guid jobPostId)
        {
            var employerId = GetEmployerId();
            if (employerId == null)
                return Unauthorized("Invalid user claim.");

            var result = await _jobPostService.ArchiveMyJobPostAsync(employerId.Value, jobPostId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { message = "Job post archived successfully." });
        }

        [Authorize(Roles = "Employer")]
        [HttpDelete("my/{jobPostId:guid}")]
        public async Task<IActionResult> DeleteMyJobPost(Guid jobPostId)
        {
            var employerId = GetEmployerId();
            if (employerId == null)
                return Unauthorized("Invalid user claim.");

            var result = await _jobPostService.DeleteMyJobPostAsync(employerId.Value, jobPostId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { message = "Job post deleted successfully." });
        }

        private Guid? GetEmployerId()
        {
            var employerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(employerIdClaim, out var employerId) ? employerId : null;
        }
    }
}