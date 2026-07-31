using Core.JobPosts;
using Core.Services;
using Microsoft.Extensions.Options;

namespace API.Services;

/// <summary>
/// Periodically expires Active job posts whose shift archive window has elapsed.
/// </summary>
public sealed class JobPostLifecycleHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<JobPostLifecycleSettings> _settings;
    private readonly ILogger<JobPostLifecycleHostedService> _logger;

    public JobPostLifecycleHostedService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<JobPostLifecycleSettings> settings,
        ILogger<JobPostLifecycleHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let startup migrations / seed finish before the first scan.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = _settings.CurrentValue;
            if (settings.Enabled)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var lifecycle = scope.ServiceProvider.GetRequiredService<IJobPostLifecycleService>();
                    var expired = await lifecycle.ExpireStaleActivePostsAsync(stoppingToken);
                    if (expired > 0)
                    {
                        _logger.LogInformation("Job post lifecycle tick expired {Count} post(s).", expired);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Job post lifecycle tick failed.");
                }
            }

            var delayMinutes = Math.Clamp(settings.IntervalMinutes, 1, 24 * 60);
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
