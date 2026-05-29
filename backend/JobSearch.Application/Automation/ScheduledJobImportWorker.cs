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
            logger.LogInformation("Scheduled job import worker is disabled.");
            return;
        }

        if (!HasRegisteredImporter())
        {
            logger.LogWarning("Scheduled job import worker is enabled, but no supported job import provider is registered.");
            return;
        }

        var interval = GetInterval(configuration);
        logger.LogInformation("Scheduled job import worker started with interval {Interval}.", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunImportAsync(stoppingToken);

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
        return scope.ServiceProvider.GetService<IJobImportService>() is not null;
    }

    private async Task RunImportAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Scheduled job import run started.");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var importer = scope.ServiceProvider.GetRequiredService<IJobImportService>();
            await importer.ImportAsync(cancellationToken);
            logger.LogInformation("Scheduled job import run completed successfully.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Scheduled job import run canceled.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Scheduled job import run failed.");
        }
    }
}
