namespace JobSearch.Domain.Entities;

public sealed class FitScoreResult
{
    public int FitScore { get; set; }
    public IReadOnlyCollection<string> MatchingSkills { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> MissingSkills { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Concerns { get; set; } = Array.Empty<string>();
    public required string RecommendedAction { get; set; }
}
