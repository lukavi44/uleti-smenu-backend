using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace API.Health;

public static class LivenessHealthEndpoint
{
    public static readonly object OkPayload = new { status = "ok" };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointConventionBuilder MapLivenessHealth(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapMethods(
            "/health",
            new[] { HttpMethods.Get, HttpMethods.Head },
            WriteLivenessAsync);
    }

    internal static async Task WriteLivenessAsync(HttpContext context)
    {
        var json = JsonSerializer.Serialize(OkPayload, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength = bytes.Length;

        // HEAD must share status/headers with GET but omit the body (RFC 9110).
        if (HttpMethods.IsHead(context.Request.Method))
            return;

        await context.Response.Body.WriteAsync(bytes);
    }
}
