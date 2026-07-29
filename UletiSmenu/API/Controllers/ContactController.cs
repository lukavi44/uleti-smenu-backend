using API.DTOs;
using API.Security;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public sealed class ContactController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactController> _logger;

    public ContactController(IEmailService emailService, ILogger<ContactController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Contact)]
    public async Task<IActionResult> Send([FromBody] ContactMessageDTO request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var sent = await _emailService.SendContactFormAsync(
            request.Name.Trim(),
            request.Email.Trim(),
            request.Subject.Trim(),
            request.Message.Trim(),
            cancellationToken);

        if (!sent)
        {
            _logger.LogWarning("Contact form email failed for {Email}", request.Email);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Unable to send your message right now. Please email support@uletismenu.com directly."
            });
        }

        return Ok(new { message = "Message sent. We will get back to you soon." });
    }
}
