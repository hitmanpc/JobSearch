namespace JobSearch.Domain.Entities;

/// <summary>
/// Single-row table that tracks the scheduled job import lifecycle.
/// Always use Id = 1.
/// </summary>
public sealed class ScheduledJobRunStatus
{
    public const int SingletonId = 1;
    public const int LastResultMaxLength = 64;
    public const int LastErrorMessageMaxLength = 2048;

    public int Id { get; set; } = SingletonId;
    public DateTimeOffset? LastRunStartedAt { get; set; }
    public DateTimeOffset? LastRunCompletedAt { get; set; }
    public bool? LastRunSucceeded { get; set; }
    public string LastResult { get; set; } = string.Empty;
    public string? LastErrorMessage { get; set; }
    public DateTimeOffset? NextExpectedRunAt { get; set; }
}
