using JobSearch.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobSearch.Application.Automation;

public sealed class ScheduledJobImportWorker : BackgroundService
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(60);
    public const string EnabledConfigurationKey = "JobImport:Enabled";
    public const string IntervalMinutesConfigurationKey = "JobImport:IntervalMinutes";

    private readonly IServiceScopeFactory scopeFactory;
    private readonly IConfiguration configuration;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ScheduledJobImportWorker> logger;

    public ScheduledJobImportWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<ScheduledJobImportWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.configuration = configuration;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!IsEnabled(configuration))
        {
            await MarkDisabledAsync(CancellationToken.None);
            logger.LogInformation("Scheduled job import worker is disabled.");
            return;
        }

        if (!HasRegisteredImporter())
        {
            await MarkNoImporterConfiguredAsync(CancellationToken.None);
            logger.LogWarning("Scheduled job import worker is enabled, but no supported job import provider is registered.");
            return;
        }

        var interval = GetInterval(configuration);
        logger.LogInformation("Scheduled job import worker started with interval {Interval}.", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            var completed = await RunImportAsync(stoppingToken);
            if (!completed || stoppingToken.IsCancellationRequested)
            {
                break;
            }

            var nextExpectedRunAt = timeProvider.GetUtcNow().Add(interval);
            await MarkNextRunScheduledAsync(nextExpectedRunAt, stoppingToken);

            try
            {
                await Task.Delay(interval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Scheduled job import worker cancellation requested while waiting for next run.");
                break;
            }
        }

        logger.LogInformation("Scheduled job import worker stopped.");
    }

    public static bool IsEnabled(IConfiguration configuration) =>
        bool.TryParse(configuration[EnabledConfigurationKey], out var enabled) && enabled;

    public static TimeSpan GetInterval(IConfiguration configuration)
    {
        if (!int.TryParse(configuration[IntervalMinutesConfigurationKey], out var intervalMinutes) || intervalMinutes <= 0)
        {
            return DefaultInterval;
        }

        return TimeSpan.FromMinutes(intervalMinutes);
    }

    private bool HasRegisteredImporter()
    {
        using var scope = scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IServiceProviderIsService>().IsService(typeof(IJobImportService));
    }

    private async Task<bool> RunImportAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Scheduled job import run started.");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var lifecycle = scope.ServiceProvider.GetRequiredService<IScheduledJobRunStatusService>();
            await lifecycle.MarkStartedAsync(cancellationToken);

            var importer = scope.ServiceProvider.GetRequiredService<IJobImportService>();
            await importer.ImportAsync(cancellationToken);

            await lifecycle.MarkSucceededAsync(cancellationToken);
            logger.LogInformation("Scheduled job import run completed successfully.");
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Scheduled job import run canceled.");
            return false;
        }
        catch (Exception exception)
        {
            await MarkFailedAsync(exception, CancellationToken.None);
            logger.LogError(exception, "Scheduled job import run failed.");
            return true;
        }
    }

    private Task MarkDisabledAsync(CancellationToken cancellationToken) =>
        UpdateLifecycleAsync(lifecycle => lifecycle.MarkDisabledAsync(cancellationToken));

    private Task MarkNoImporterConfiguredAsync(CancellationToken cancellationToken) =>
        UpdateLifecycleAsync(lifecycle => lifecycle.MarkNoImporterConfiguredAsync(cancellationToken));

    private Task MarkFailedAsync(Exception exception, CancellationToken cancellationToken) =>
        UpdateLifecycleAsync(lifecycle => lifecycle.MarkFailedAsync(exception, cancellationToken));

    private Task MarkNextRunScheduledAsync(DateTimeOffset nextExpectedRunAt, CancellationToken cancellationToken) =>
        UpdateLifecycleAsync(lifecycle => lifecycle.MarkNextRunScheduledAsync(nextExpectedRunAt, cancellationToken));

    private async Task UpdateLifecycleAsync(Func<IScheduledJobRunStatusService, Task> update)
    {
        using var scope = scopeFactory.CreateScope();
        var lifecycle = scope.ServiceProvider.GetRequiredService<IScheduledJobRunStatusService>();
        await update(lifecycle);
    }
}
