using Core.Models.Entities;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace API.Middlewares
{
    public class AuthenticationAuditMiddleware
    {
        private const long MaxAuditedBodyBytes = 16 * 1024;
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationAuditMiddleware> _logger;

        public AuthenticationAuditMiddleware(
            RequestDelegate next,
            ILogger<AuthenticationAuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            UserManager<User> userManager)
        {
            string? email = null;
            if (HttpMethods.IsPost(context.Request.Method) &&
                context.Request.Path.Equals("/login", StringComparison.OrdinalIgnoreCase) &&
                context.Request.ContentLength is > 0 and <= MaxAuditedBodyBytes)
            {
                email = await ReadEmailAsync(context.Request, context.RequestAborted);
            }

            await _next(context);

            if (email == null ||
                context.Response.StatusCode != StatusCodes.Status401Unauthorized)
            {
                return;
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user != null && await userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning(
                    "Identity account locked after failed authentication attempts. UserId={UserId} ClientIp={ClientIp} TraceId={TraceId}",
                    user.Id,
                    context.Connection.RemoteIpAddress,
                    context.TraceIdentifier);
            }
        }

        private static async Task<string?> ReadEmailAsync(
            HttpRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.EnableBuffering();
                using var document = await JsonDocument.ParseAsync(
                    request.Body,
                    cancellationToken: cancellationToken);
                request.Body.Position = 0;

                return document.RootElement.TryGetProperty("email", out var emailElement)
                    ? emailElement.GetString()?.Trim()
                    : null;
            }
            catch (JsonException)
            {
                request.Body.Position = 0;
                return null;
            }
        }
    }
}
