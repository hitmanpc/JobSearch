namespace JobSearch.Application.Services;

public interface ICandidateProfileService
{
    Task<CandidateProfileSettingsSnapshot> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(CandidateProfileSettingsSnapshot settings, CancellationToken cancellationToken = default);
    Task<string> GetResumeTextAsync(CancellationToken cancellationToken = default);
    Task SaveResumeTextAsync(string resumeText, CancellationToken cancellationToken = default);
}

public sealed record CandidateProfileSettingsSnapshot(
    string ResumeText,
    string? RemotiveCategory,
    string? RemotiveSearchText,
    int? RemotiveLimit);
