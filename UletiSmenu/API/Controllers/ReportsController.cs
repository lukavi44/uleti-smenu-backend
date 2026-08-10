using API.Security;
using Core.Models.Enums;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/Reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Contact)]
    public async Task<IActionResult> Submit([FromBody] SubmitReportRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        if (!Enum.TryParse<ReportTargetType>(request.TargetType, true, out var targetType))
            return BadRequest(new { message = "Invalid target type." });

        var result = await _reportService.SubmitAsync(
            userId,
            targetType,
            request.TargetId,
            request.Reason,
            request.Details,
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Report submitted." });
    }
}

public sealed class SubmitReportRequest
{
    [Required]
    [MaxLength(32)]
    public string TargetType { get; set; } = string.Empty;

    [Required]
    public Guid TargetId { get; set; }

    [Required]
    [MinLength(3)]
    [MaxLength(80)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Details { get; set; }
}
