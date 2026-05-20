using JobSearch.Application.Services;
using JobSearch.Domain.Entities;
using Xunit;

namespace JobSearch.Tests;

public sealed class MockFitScoringServiceTests
{
    [Fact]
    public async Task ScoreAsync_ReturnsMatchingAndMissingSkills()
    {
        var service = new MockFitScoringService();
        var job = new JobOpportunity
        {
            Company = "Acme",
            Title = "Senior Full Stack Engineer",
            Description = "Build Angular, .NET, C#, Docker, SQL, and GitHub Actions workflows."
        };

        var result = await service.ScoreAsync(job);

        Assert.Equal(55, result.FitScore);
        Assert.Contains("Angular", result.MatchingSkills);
        Assert.Contains(".NET", result.MatchingSkills);
        Assert.Contains("C#", result.MatchingSkills);
        Assert.Contains("Docker", result.MatchingSkills);
        Assert.Contains("SQL", result.MatchingSkills);
        Assert.Contains("GitHub Actions", result.MatchingSkills);
        Assert.Contains("React", result.MissingSkills);
        Assert.Equal("Review before applying", result.RecommendedAction);
    }

    [Fact]
    public async Task ScoreAsync_AddsConcernsWhenDescriptionIsMissing()
    {
        var service = new MockFitScoringService();
        var job = new JobOpportunity
        {
            Company = "Acme",
            Title = "Generalist Engineer",
            Description = null
        };

        var result = await service.ScoreAsync(job);

        Assert.Equal(0, result.FitScore);
        Assert.Empty(result.MatchingSkills);
        Assert.Equal("Deprioritize", result.RecommendedAction);
        Assert.Contains("No job description is available to evaluate.", result.Concerns);
        Assert.Contains("No target skills were found in the job details.", result.Concerns);
    }
}
