using API.DTOs;
using AutoMapper;
using Core.Models.Entities;
using Core.Services;
using Infrastructure.Persistence.Database;
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

        [HttpPost("createJobPost")]
        public async Task<IActionResult> CreateJobPost([FromBody] JobPostDTO jobPostDTO)
        {
            var employerIdClaim = User.FindFirstValue("userId");
            var employerIdClaim2 = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(employerIdClaim))
                return Unauthorized("Employer ID claim not found");

            var employerId = Guid.Parse(employerIdClaim);

            var jobPost = _mapper.Map<JobPost>(jobPostDTO, opt =>
            {
                opt.Items["EmployerId"] = employerId;
            });

            var result = await _jobPostService.CreateJobPostAsync(jobPost);

            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { message = "Job post created successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllJobPosts()
        {
            var jobPosts = await _jobPostService.GetAllJobPostsAsync();
            return Ok(jobPosts);
        }
    }
}