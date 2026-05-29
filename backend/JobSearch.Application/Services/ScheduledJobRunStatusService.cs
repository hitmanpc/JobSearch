using JobSearch.Application.Persistence;
using JobSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearch.Application.Services;

public sealed class ScheduledJobRunStatusService : IScheduledJobRunStatusService
{
    public const string DisabledResult = "Disabled";
    public const string NoImporterConfiguredResult = "NoImporterConfigured";
    public const string StartedResult = "Started";
    public const string SucceededResult = "Succeeded";
    public const string FailedResult = "Failed";
    private readonly AppDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public ScheduledJobRunStatusService(AppDbContext dbContext, TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public Task MarkDisabledAsync(CancellationToken cancellationToken = default) =>
        UpdateAsync(
            status =>
            {
                status.LastRunSucceeded = false;
                status.LastResult = DisabledResult;
                status.LastErrorMessage = null;
                status.NextExpectedRunAt = null;
            },
            cancellationToken);

    public Task MarkNoImporterConfiguredAsync(CancellationToken cancellationToken = default) =>
        UpdateAsync(
            status =>
            {
                status.LastRunSucceeded = false;
                status.LastResult = NoImporterConfiguredResult;
                status.LastErrorMessage = null;
                status.NextExpectedRunAt = null;
            },
            cancellationToken);

    public Task MarkStartedAsync(CancellationToken cancellationToken = default) =>
        UpdateAsync(
            status =>
            {
                status.LastRunStartedAt = timeProvider.GetUtcNow();
                status.LastRunCompletedAt = null;
                status.LastRunSucceeded = null;
                status.LastResult = StartedResult;
                status.LastErrorMessage = null;
            },
            cancellationToken);

    public Task MarkSucceededAsync(CancellationToken cancellationToken = default) =>
        UpdateAsync(
            status =>
            {
                status.LastRunCompletedAt = timeProvider.GetUtcNow();
                status.LastRunSucceeded = true;
                status.LastResult = SucceededResult;
                status.LastErrorMessage = null;
            },
            cancellationToken);

    public Task MarkFailedAsync(Exception exception, CancellationToken cancellationToken = default) =>
        UpdateAsync(
            status =>
            {
                status.LastRunCompletedAt = timeProvider.GetUtcNow();
                status.LastRunSucceeded = false;
                status.LastResult = FailedResult;
                status.LastErrorMessage = TruncateError(exception.Message);
            },
            cancellationToken);

    public Task MarkNextRunScheduledAsync(DateTimeOffset nextExpectedRunAt, CancellationToken cancellationToken = default) =>
        UpdateAsync(
            status => status.NextExpectedRunAt = nextExpectedRunAt,
            cancellationToken);

    private async Task UpdateAsync(Action<ScheduledJobRunStatus> update, CancellationToken cancellationToken)
    {
        var status = await dbContext.ScheduledJobRunStatuses
            .SingleOrDefaultAsync(x => x.Id == ScheduledJobRunStatus.SingletonId, cancellationToken);

        if (status is null)
        {
            status = new ScheduledJobRunStatus { Id = ScheduledJobRunStatus.SingletonId };
            dbContext.ScheduledJobRunStatuses.Add(status);
        }

        update(status);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string TruncateError(string errorMessage)
    {
        if (errorMessage.Length <= ScheduledJobRunStatus.LastErrorMessageMaxLength)
        {
            return errorMessage;
        }

        return errorMessage[..ScheduledJobRunStatus.LastErrorMessageMaxLength];
    }
}
