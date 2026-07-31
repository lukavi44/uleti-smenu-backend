using System.Net;
using System.Net.Http;
using System.Text;
using API.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UletiSmenu.Tests.IntegrationTests;

public class LivenessHealthEndpointTests
{
    [Fact]
    public async Task Get_Health_Returns200WithOkPayload()
    {
        await using var host = await CreateHostAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ok", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("status", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Head_Health_Returns200WithEmptyBody()
    {
        await using var host = await CreateHostAsync();
        var client = host.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Head, "/health");
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(string.IsNullOrEmpty(body), $"HEAD body should be empty, got: '{body}'");
    }

    [Fact]
    public async Task Post_Health_Returns405()
    {
        await using var host = await CreateHostAsync();
        var client = host.GetTestClient();

        var response = await client.PostAsync("/health", new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.True(
            response.Headers.TryGetValues("Allow", out var allow) ||
            response.Content.Headers.TryGetValues("Allow", out allow),
            "Expected Allow header on 405 response.");
        Assert.Contains(allow!, value => value.Contains("GET", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(allow!, value => value.Contains("HEAD", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<WebApplication> CreateHostAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.MapLivenessHealth();
        await app.StartAsync();
        return app;
    }
}
