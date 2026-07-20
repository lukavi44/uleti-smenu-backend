using API.Middlewares;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace UletiSmenu.Tests.Controllers;

public class ExceptionHandlingMiddlewareTests
{
    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task NonDevelopmentEnvironment_DoesNotExposeExceptionDetails(string environmentName)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(environmentName);
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ArgumentException("sensitive database detail"),
            Mock.Of<ILogger<ExceptionHandlingMiddleware>>(),
            environment.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.Invoke(context);

        context.Response.Body.Position = 0;
        using var payload = await JsonDocument.ParseAsync(context.Response.Body);
        var message = payload.RootElement.GetProperty("Message").GetString();

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("The request was invalid.", message);
        Assert.DoesNotContain("sensitive", message, StringComparison.OrdinalIgnoreCase);
    }
}
