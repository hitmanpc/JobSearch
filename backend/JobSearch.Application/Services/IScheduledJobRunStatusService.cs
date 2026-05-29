using JobSearch.Domain.Entities;

namespace JobSearch.Application.Services;

public interface IScheduledJobRunStatusService
{
    Task<ScheduledJobRunStatus> GetAsync(CancellationToken cancellationToken = default);
    Task MarkDisabledAsync(CancellationToken cancellationToken = default);
    Task MarkNoImporterConfiguredAsync(CancellationToken cancellationToken = default);
    Task MarkStartedAsync(CancellationToken cancellationToken = default);
    Task MarkSucceededAsync(CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Exception exception, CancellationToken cancellationToken = default);
    Task MarkNextRunScheduledAsync(DateTimeOffset nextExpectedRunAt, CancellationToken cancellationToken = default);
}
