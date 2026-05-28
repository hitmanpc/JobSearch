using JobSearch.Domain.Entities;

namespace JobSearch.Application.Services;

public interface IFitScoringService
{
    Task<FitScoreResult> ScoreAsync(JobOpportunity job, string resumeText, CancellationToken cancellationToken = default);
}
