using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PlatformController : ControllerBase
    {
        private readonly IPlatformStatsService _platformStatsService;

        public PlatformController(IPlatformStatsService platformStatsService)
        {
            _platformStatsService = platformStatsService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetPublicStats()
        {
            var stats = await _platformStatsService.GetPublicStatsAsync();
            return Ok(stats);
        }
    }
}
