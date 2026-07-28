using API.Startup;

namespace API.Services;

public sealed class ApplicationStartupHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ApplicationStartupHostedService> _logger;

    public ApplicationStartupHostedService(
        IServiceProvider services,
        ILogger<ApplicationStartupHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = InitializeInBackgroundAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task InitializeInBackgroundAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Running background startup initialization.");
            await ApplicationStartupInitializer.InitializeAsync(_services, cancellationToken);
            _logger.LogInformation("Background startup initialization completed.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Background startup initialization failed.");
        }
    }
}
