using API.DTOs;
using AutoMapper;
using Core.Models.Entities;
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
        private readonly UserManager<User> _userManager;
        

        public JobPostController(ApplicationDbContext context, IMapper mapper, IUserService userService, IJobPostService jobPostService, UserManager<User> userManager)
        {
            _context = context;
            _mapper = mapper;
            _userService = userService;
            _jobPostService = jobPostService;
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
        public async Task<IActionResult> GetMyJobPosts()
        {
            var employerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(employerIdClaim, out var employerId))
                return Unauthorized("Invalid user claim.");

            var user = await _userService.GetUserByIdAsync(employerId);
            if (user is not Employer)
                return Forbid();

            var jobPosts = await _jobPostService.GetMyJobPostsAsync(employerId);
            var jobPostDtos = _mapper.Map<List<JobPostDTO>>(jobPosts);

            return Ok(jobPostDtos);
        }
    }
}