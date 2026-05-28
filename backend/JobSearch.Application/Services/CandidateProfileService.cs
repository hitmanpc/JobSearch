using JobSearch.Application.Persistence;
using JobSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearch.Application.Services;

public sealed class CandidateProfileService(AppDbContext dbContext) : ICandidateProfileService
{
    private const int ProfileId = 1;

    public async Task<string> GetResumeTextAsync(CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.CandidateProfile
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ProfileId, cancellationToken);

        return profile?.ResumeText ?? string.Empty;
    }

    public async Task SaveResumeTextAsync(string resumeText, CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.CandidateProfile
            .FirstOrDefaultAsync(x => x.Id == ProfileId, cancellationToken);

        if (profile is null)
        {
            profile = new CandidateProfileSettings { Id = ProfileId, ResumeText = resumeText };
            dbContext.CandidateProfile.Add(profile);
        }
        else
        {
            profile.ResumeText = resumeText;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
