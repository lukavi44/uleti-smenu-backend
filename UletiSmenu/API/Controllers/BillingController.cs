using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Employer")]
    [Route("api/v1/[controller]")]
    public class BillingController : ControllerBase
    {
        private readonly IBillingService _billingService;
        private readonly IConfiguration _configuration;

        public BillingController(IBillingService billingService, IConfiguration configuration)
        {
            _billingService = billingService;
            _configuration = configuration;
        }

        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans()
        {
            var plans = await _billingService.GetAvailablePaidPlansAsync();
            return Ok(plans);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyBilling()
        {
            var employerId = GetCurrentUserId();
            if (employerId == null)
                return Unauthorized();

            var subscription = await _billingService.GetSubscriptionStatusAsync(employerId.Value);
            var plans = await _billingService.GetAvailablePaidPlansAsync();

            return Ok(new
            {
                subscription,
                plans,
                paymentsEnabled = false,
                message = "Online payments are not enabled yet. Contact support@uletismenu.com to upgrade manually until Stripe checkout is live."
            });
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
