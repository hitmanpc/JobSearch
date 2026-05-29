using Microsoft.Extensions.Logging;

namespace JobSearch.Application.Automation;

public sealed class NoOpJobImportService : IJobImportService
{
    private readonly ILogger<NoOpJobImportService> logger;

    public NoOpJobImportService(ILogger<NoOpJobImportService> logger)
    {
        this.logger = logger;
    }

    public Task ImportAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Scheduled job import ran with the default no-op importer. Configure a real importer to add jobs.");
        return Task.CompletedTask;
    }
}
