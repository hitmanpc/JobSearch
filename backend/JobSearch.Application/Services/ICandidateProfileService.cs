namespace JobSearch.Application.Services;

public interface ICandidateProfileService
{
    Task<string> GetResumeTextAsync(CancellationToken cancellationToken = default);
    Task SaveResumeTextAsync(string resumeText, CancellationToken cancellationToken = default);
}
