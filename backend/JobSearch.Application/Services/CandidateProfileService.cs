using JobSearch.Application.Persistence;
using JobSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearch.Application.Services;

public sealed class CandidateProfileService(AppDbContext dbContext) : ICandidateProfileService
{
    private const int ProfileId = 1;

    public async Task<CandidateProfileSettingsSnapshot> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.CandidateProfile
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ProfileId, cancellationToken);

        return profile is null
            ? new CandidateProfileSettingsSnapshot(string.Empty, null, null, null)
            : new CandidateProfileSettingsSnapshot(
                profile.ResumeText,
                profile.RemotiveCategory,
                profile.RemotiveSearchText,
                profile.RemotiveLimit);
    }

    public async Task SaveSettingsAsync(CandidateProfileSettingsSnapshot settings, CancellationToken cancellationToken = default)
    {
        if (settings.RemotiveLimit is <= 0)
        {
            throw new InvalidOperationException("Remotive limit must be a positive integer when provided.");
        }

        var profile = await dbContext.CandidateProfile
            .FirstOrDefaultAsync(x => x.Id == ProfileId, cancellationToken);

        if (profile is null)
        {
            profile = new CandidateProfileSettings { Id = ProfileId };
            dbContext.CandidateProfile.Add(profile);
        }

        profile.ResumeText = settings.ResumeText;
        profile.RemotiveCategory = NormalizeOptionalText(settings.RemotiveCategory);
        profile.RemotiveSearchText = NormalizeOptionalText(settings.RemotiveSearchText);
        profile.RemotiveLimit = settings.RemotiveLimit;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GetResumeTextAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return settings.ResumeText;
    }

    public async Task SaveResumeTextAsync(string resumeText, CancellationToken cancellationToken = default)
    {
        var current = await GetSettingsAsync(cancellationToken);
        await SaveSettingsAsync(current with { ResumeText = resumeText }, cancellationToken);
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
