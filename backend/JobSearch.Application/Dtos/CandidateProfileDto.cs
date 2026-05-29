namespace JobSearch.Application.Dtos;

public sealed record CandidateProfileRequestDto(string? ResumeText);

public sealed record CandidateProfileResponseDto(
    string ResumeText,
    JobImportStatusDto JobImportStatus);

public sealed record JobImportStatusDto(
    bool WorkerEnabled,
    int ConfiguredIntervalMinutes,
    DateTimeOffset? LastRunStartedAt,
    DateTimeOffset? LastRunCompletedAt,
    bool? LastRunSucceeded,
    string LastResult,
    string? LastErrorMessage,
    DateTimeOffset? NextExpectedRunAt);
