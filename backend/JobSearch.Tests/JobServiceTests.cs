using JobSearch.Application.Dtos;
using JobSearch.Application.Repositories;
using JobSearch.Application.Services;
using JobSearch.Domain.Enums;
using Xunit;

namespace JobSearch.Tests;

public sealed class JobServiceTests
{
    [Fact]
    public async Task CreateJobAsync_CreatesJobWithFoundStatus()
    {
        var service = CreateService();
        var request = new CreateJobRequest(
            "Acme",
            "Senior Full Stack Engineer",
            "Chicago, IL",
            RemoteType.Hybrid,
            "https://example.com/jobs/1",
            "Build useful things.",
            91);

        var job = await service.CreateJobAsync(request);

        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal("Acme", job.Company);
        Assert.Equal("Senior Full Stack Engineer", job.Title);
        Assert.Equal(ApplicationStatus.Found, job.Status);
        Assert.Equal(91, job.FitScore);
    }

    [Fact]
    public async Task UpdateStatusAsync_ChangesJobStatus()
    {
        var service = CreateService();
        var job = await service.CreateJobAsync(new CreateJobRequest(
            "Acme",
            "Senior Full Stack Engineer",
            null,
            RemoteType.Remote,
            null,
            null,
            null));

        var updated = await service.UpdateStatusAsync(
            job.Id,
            new UpdateJobStatusRequest(ApplicationStatus.Interested));

        Assert.NotNull(updated);
        Assert.Equal(ApplicationStatus.Interested, updated.Status);
    }

    [Fact]
    public async Task GetJobsAsync_ReturnsCreatedJobs()
    {
        var service = CreateService();
        await service.CreateJobAsync(new CreateJobRequest("Acme", "Engineer", null, RemoteType.Remote, null, null, 80));
        await service.CreateJobAsync(new CreateJobRequest("Contoso", "Platform Engineer", null, RemoteType.Onsite, null, null, 70));

        var jobs = await service.GetJobsAsync();

        Assert.Equal(2, jobs.Count);
        Assert.Contains(jobs, job => job.Company == "Acme");
        Assert.Contains(jobs, job => job.Company == "Contoso");
    }

    [Fact]
    public async Task ScoreFitAsync_StoresScoreResultOnJob()
    {
        var service = CreateService();
        var job = await service.CreateJobAsync(new CreateJobRequest(
            "Acme",
            "Senior Angular Engineer",
            null,
            RemoteType.Remote,
            null,
            "Build Angular, .NET, Docker, CI/CD, SQL, and MongoDB systems.",
            null));

        var scoreResult = await service.ScoreFitAsync(job.Id);
        var updatedJob = await service.GetJobAsync(job.Id);

        Assert.NotNull(scoreResult);
        Assert.NotNull(updatedJob);
        Assert.Equal(scoreResult.FitScore, updatedJob.FitScore);
        Assert.NotNull(updatedJob.FitScoreResult);
        Assert.Equal(scoreResult.FitScore, updatedJob.FitScoreResult.FitScore);
        Assert.Contains("Angular", updatedJob.FitScoreResult.MatchingSkills);
        Assert.Contains("React", updatedJob.FitScoreResult.MissingSkills);
    }

    [Fact]
    public async Task GenerateRecruiterMessageAsync_IncludesCompanyTitleSkillsAndPortfolio()
    {
        var service = CreateService();
        var job = await service.CreateJobAsync(new CreateJobRequest(
            "Acme",
            "Senior Full Stack Engineer",
            null,
            RemoteType.Remote,
            null,
            "Build systems.",
            null));

        await service.ScoreFitAsync(job.Id);

        var generated = await service.GenerateRecruiterMessageAsync(job.Id);

        Assert.NotNull(generated);
        Assert.Contains("Acme", generated.Message);
        Assert.Contains("Senior Full Stack Engineer", generated.Message);
        Assert.Contains("Angular", generated.Message);
        Assert.Contains(".NET", generated.Message);
        Assert.Contains("donbowman.info", generated.Message);
    }

    private static JobService CreateService()
    {
        return new JobService(new InMemoryJobRepository(), new MockFitScoringService(), TimeProvider.System);
    }
}
