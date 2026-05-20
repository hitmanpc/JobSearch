using JobSearch.Domain.Entities;

namespace JobSearch.Application.Dtos;

public sealed record FitScoreResultDto(
    int FitScore,
    IReadOnlyCollection<string> MatchingSkills,
    IReadOnlyCollection<string> MissingSkills,
    IReadOnlyCollection<string> Concerns,
    string RecommendedAction)
{
    public static FitScoreResultDto FromEntity(FitScoreResult result) =>
        new(
            result.FitScore,
            result.MatchingSkills,
            result.MissingSkills,
            result.Concerns,
            result.RecommendedAction);
}
