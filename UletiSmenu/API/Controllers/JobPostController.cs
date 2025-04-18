using API.DTOs;
using AutoMapper;
using Core.Models.Entities;
using Core.Services;
using Infrastructure.Persistence.Database;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/[jobPost]")]
    public class JobPostController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public JobPostController(ApplicationDbContext context, IMapper mapper, IUserService userService)
        {
            _context = context;
            _mapper = mapper;
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateJobPost([FromBody] JobPostDTO jobPostDTO)
        {
            var employer = await _userService.GetUserByIdAsync(jobPostDTO.EmployerId);
            if (employer == null)
                return NotFound("Employer not found.");

            var jobPost = _mapper.Map<JobPost>(jobPostDTO);

            var result = await _jobPostService.CreateAsync(jobPost);
        }
    }
}