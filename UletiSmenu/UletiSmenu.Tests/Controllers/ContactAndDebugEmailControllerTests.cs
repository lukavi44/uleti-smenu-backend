using API.Controllers;
using API.Filters;
using API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;

namespace UletiSmenu.Tests.Controllers;

public class ContactAndDebugEmailControllerTests
{
    [Fact]
    public void ContactEndpointIsRateLimitedAndAnonymous()
    {
        var method = typeof(ContactController).GetMethod(nameof(ContactController.Send));
        Assert.NotNull(method);

        var rateLimit = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(rateLimit);
        Assert.Equal(RateLimitPolicies.Contact, rateLimit!.PolicyName);

        var allowAnonymous = typeof(ContactController).GetCustomAttribute<AllowAnonymousAttribute>()
            ?? method.GetCustomAttribute<AllowAnonymousAttribute>();
        Assert.NotNull(allowAnonymous);
    }

    [Fact]
    public void DebugTestEmailRouteIsAnonymousDevOrStagingOnly()
    {
        var method = typeof(DebugEmailController).GetMethod(nameof(DebugEmailController.SendTestEmail));
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<HttpPostAttribute>());
        Assert.Equal("X-Email-Debug-Key", DebugEmailController.DebugKeyHeader);
        Assert.NotNull(typeof(DebugEmailController).GetCustomAttribute<DevelopmentOrStagingOnlyAttribute>());
    }
}
