using API.DTOs;
using API.Security;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public sealed class ContactController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly ILogger<ContactController> _logger;

    public ContactController(IContactService contactService, ILogger<ContactController> logger)
    {
        _contactService = contactService;
        _logger = logger;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Contact)]
    public async Task<IActionResult> Send([FromBody] ContactMessageDTO request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _contactService.SubmitAsync(
            request.Name,
            request.Email,
            request.Subject,
            request.Message,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Contact form submit failed: {Error}", result.Error);
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "Message sent. We will get back to you soon." });
    }
}
