namespace JobSearch.Application.Dtos;

public sealed record CandidateProfileRequestDto(
    string? ResumeText,
    string? RemotiveCategory,
    string? RemotiveSearchText,
    int? RemotiveLimit);

public sealed record CandidateProfileResponseDto(
    string ResumeText,
    string? RemotiveCategory,
    string? RemotiveSearchText,
    int? RemotiveLimit,
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
