using JobSearch.Domain.Enums;

namespace JobSearch.Domain.Entities;

public sealed class JobOpportunity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Company { get; set; }
    public required string Title { get; set; }
    public string? Location { get; set; }
    public RemoteType RemoteType { get; set; } = RemoteType.Unknown;
    public string? Url { get; set; }
    public string? Description { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Found;
    public int? FitScore { get; set; }
    public DateTimeOffset DateFound { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DateApplied { get; set; }
}
