using API.DTOs;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class BillingController : ControllerBase
    {
        private readonly IBillingService _billingService;
        private readonly IPaymentProvider _paymentProvider;

        public BillingController(IBillingService billingService, IPaymentProvider paymentProvider)
        {
            _billingService = billingService;
            _paymentProvider = paymentProvider;
        }

        [HttpGet("plans")]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> GetPlans()
        {
            var plans = await _billingService.GetAvailablePaidPlansAsync();
            return Ok(plans);
        }

        [HttpGet("me")]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> GetMyBilling()
        {
            var employerId = GetCurrentUserId();
            if (employerId == null)
                return Unauthorized();

            var subscription = await _billingService.GetSubscriptionStatusAsync(employerId.Value);
            var plans = await _billingService.GetAvailablePaidPlansAsync();
            var paymentsEnabled = _billingService.IsPaymentsEnabled();

            return Ok(new
            {
                subscription,
                plans,
                paymentsEnabled,
                message = paymentsEnabled
                    ? string.Empty
                    : "Online payments are not enabled. Set Stripe:Enabled and API keys to activate checkout."
            });
        }

        [HttpPost("checkout")]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> CreateCheckout([FromBody] BillingCheckoutRequest request)
        {
            var employerId = GetCurrentUserId();
            if (employerId == null)
                return Unauthorized();

            var result = await _billingService.CreateCheckoutSessionAsync(
                employerId.Value,
                request.PlanId,
                request.SuccessUrl,
                request.CancelUrl);

            if (result.IsFailure)
                return BadRequest(new { message = result.Error });

            return Ok(new { checkoutUrl = result.Value });
        }

        [HttpPost("portal")]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> CreatePortal([FromBody] BillingPortalRequest request)
        {
            var employerId = GetCurrentUserId();
            if (employerId == null)
                return Unauthorized();

            var result = await _billingService.CreateCustomerPortalSessionAsync(employerId.Value, request.ReturnUrl);
            if (result.IsFailure)
                return BadRequest(new { message = result.Error });

            return Ok(new { portalUrl = result.Value });
        }

        [HttpPost("webhooks/stripe")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            var result = await _paymentProvider.HandleWebhookAsync(json, signature);
            if (result.IsFailure)
                return BadRequest(new { message = result.Error });

            return Ok();
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
