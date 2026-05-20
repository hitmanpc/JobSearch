using JobSearch.Domain.Entities;

namespace JobSearch.Application.Services;

public sealed class MockFitScoringService : IFitScoringService
{
    private static readonly IReadOnlyCollection<string> TargetSkills =
    [
        "Angular",
        "React",
        ".NET",
        "C#",
        "Docker",
        "CI/CD",
        "GitHub Actions",
        "microfrontends",
        "single-spa",
        "SQL",
        "MongoDB"
    ];

    public Task<FitScoreResult> ScoreAsync(JobOpportunity job, CancellationToken cancellationToken = default)
    {
        var searchableText = string.Join(
            ' ',
            job.Title,
            job.Company,
            job.Description,
            job.Location);

        var matchingSkills = TargetSkills
            .Where(skill => ContainsSkill(searchableText, skill))
            .ToArray();
        var missingSkills = TargetSkills.Except(matchingSkills).ToArray();
        var fitScore = CalculateFitScore(matchingSkills.Length);
        var concerns = BuildConcerns(job, matchingSkills.Length);

        var result = new FitScoreResult
        {
            FitScore = fitScore,
            MatchingSkills = matchingSkills,
            MissingSkills = missingSkills,
            Concerns = concerns,
            RecommendedAction = GetRecommendedAction(fitScore, concerns.Count)
        };

        return Task.FromResult(result);
    }

    private static bool ContainsSkill(string searchableText, string skill) =>
        searchableText.Contains(skill, StringComparison.OrdinalIgnoreCase);

    private static int CalculateFitScore(int matchingSkillCount) =>
        (int)Math.Round((double)matchingSkillCount / TargetSkills.Count * 100);

    private static IReadOnlyCollection<string> BuildConcerns(JobOpportunity job, int matchingSkillCount)
    {
        var concerns = new List<string>();

        if (string.IsNullOrWhiteSpace(job.Description))
        {
            concerns.Add("No job description is available to evaluate.");
        }

        if (matchingSkillCount == 0)
        {
            concerns.Add("No target skills were found in the job details.");
        }
        else if (matchingSkillCount < 4)
        {
            concerns.Add("Only a few target skills were found.");
        }

        return concerns;
    }

    private static string GetRecommendedAction(int fitScore, int concernCount) =>
        fitScore switch
        {
            >= 75 when concernCount == 0 => "Apply",
            >= 50 => "Review before applying",
            _ => "Deprioritize"
        };
}
