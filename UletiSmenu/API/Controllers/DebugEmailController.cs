using API.DTOs;
using Core.Interfaces;
using Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

/// <summary>
/// SMTP smoke-test endpoint. Available only in Development and Staging (Render TEST).
/// Staging requires header X-Email-Debug-Key matching SmtpSettings:DebugApiKey.
/// Never registered for Production (returns 404).
/// </summary>
[ApiController]
[Route("api/v1/debug")]
[AllowAnonymous]
public sealed class DebugEmailController : ControllerBase
{
    public const string DebugKeyHeader = "X-Email-Debug-Key";

    private readonly IEmailService _emailService;
    private readonly IHostEnvironment _environment;
    private readonly SmtpSettings _smtpSettings;

    public DebugEmailController(
        IEmailService emailService,
        IHostEnvironment environment,
        IOptions<SmtpSettings> smtpOptions)
    {
        _emailService = emailService;
        _environment = environment;
        _smtpSettings = smtpOptions.Value;
    }

    [HttpPost("test-email")]
    public async Task<IActionResult> SendTestEmail(
        [FromBody] TestEmailDTO request,
        CancellationToken cancellationToken)
    {
        // Render TEST sets ASPNETCORE_ENVIRONMENT=Staging (see render-test.yaml).
        // Production Azure sets Production — this branch must stay closed there.
        if (_environment.IsProduction() || (!_environment.IsDevelopment() && !_environment.IsStaging()))
            return NotFound();

        if (_environment.IsStaging())
        {
            if (string.IsNullOrWhiteSpace(_smtpSettings.DebugApiKey))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "SmtpSettings__DebugApiKey is required in Staging before test-email can be used."
                });
            }

            if (!Request.Headers.TryGetValue(DebugKeyHeader, out var provided)
                || !string.Equals(provided.ToString(), _smtpSettings.DebugApiKey, StringComparison.Ordinal))
            {
                return Unauthorized(new { message = $"Missing or invalid {DebugKeyHeader} header." });
            }
        }

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var sent = await _emailService.SendSmtpTestAsync(request.Email.Trim(), cancellationToken);

        if (!sent)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "SMTP send failed. Check SmtpSettings and application logs."
            });
        }

        return Ok(new { message = $"Test email sent to {request.Email.Trim()}." });
    }
}
