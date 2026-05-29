using JobSearch.Api.Controllers;
using JobSearch.Application.Dtos;
using JobSearch.Application.Services;
using JobSearch.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace JobSearch.Tests;

public sealed class CandidateProfileControllerTests
{
    [Fact]
    public async Task GetAsync_ReturnsProfileWithJobImportStatus()
    {
        var startedAt = new DateTimeOffset(2026, 5, 29, 10, 0, 0, TimeSpan.Zero);
        var completedAt = new DateTimeOffset(2026, 5, 29, 10, 1, 0, TimeSpan.Zero);
        var nextExpectedRunAt = new DateTimeOffset(2026, 5, 29, 10, 31, 0, TimeSpan.Zero);
        var statusService = new StubScheduledJobRunStatusService(new ScheduledJobRunStatus
        {
            LastRunStartedAt = startedAt,
            LastRunCompletedAt = completedAt,
            LastRunSucceeded = true,
            LastResult = ScheduledJobRunStatusService.SucceededResult,
            LastErrorMessage = null,
            NextExpectedRunAt = nextExpectedRunAt
        });
        var controller = new CandidateProfileController(
            new StubCandidateProfileService(new CandidateProfileSettingsSnapshot("Senior full-stack engineer resume", "Software Development", "angular", 25)),
            statusService,
            CreateConfiguration(enabled: true, intervalMinutes: 30));

        var response = await controller.GetAsync(CancellationToken.None);

        Assert.Equal("Senior full-stack engineer resume", response.ResumeText);
        Assert.Equal("Software Development", response.RemotiveCategory);
        Assert.Equal("angular", response.RemotiveSearchText);
        Assert.Equal(25, response.RemotiveLimit);
        Assert.True(response.JobImportStatus.WorkerEnabled);
        Assert.Equal(30, response.JobImportStatus.ConfiguredIntervalMinutes);
        Assert.Equal(startedAt, response.JobImportStatus.LastRunStartedAt);
        Assert.Equal(completedAt, response.JobImportStatus.LastRunCompletedAt);
        Assert.True(response.JobImportStatus.LastRunSucceeded);
        Assert.Equal(ScheduledJobRunStatusService.SucceededResult, response.JobImportStatus.LastResult);
        Assert.Null(response.JobImportStatus.LastErrorMessage);
        Assert.Equal(nextExpectedRunAt, response.JobImportStatus.NextExpectedRunAt);
        Assert.Equal(1, statusService.GetCallCount);
    }

    [Fact]
    public async Task PutAsync_OnlyAcceptsEditableProfileFields()
    {
        var candidateProfileService = new StubCandidateProfileService();
        var statusService = new StubScheduledJobRunStatusService(new ScheduledJobRunStatus
        {
            LastResult = ScheduledJobRunStatusService.FailedResult,
            LastErrorMessage = "Import failed"
        });
        var controller = new CandidateProfileController(
            candidateProfileService,
            statusService,
            CreateConfiguration(enabled: true, intervalMinutes: 15));

        await controller.PutAsync(new CandidateProfileRequestDto("Updated resume", "Software Development", "c#", 50), CancellationToken.None);

        Assert.NotNull(candidateProfileService.SavedSettings);
        Assert.Equal("Updated resume", candidateProfileService.SavedSettings.ResumeText);
        Assert.Equal("Software Development", candidateProfileService.SavedSettings.RemotiveCategory);
        Assert.Equal("c#", candidateProfileService.SavedSettings.RemotiveSearchText);
        Assert.Equal(50, candidateProfileService.SavedSettings.RemotiveLimit);
        Assert.Equal(0, statusService.GetCallCount);
        Assert.Null(typeof(CandidateProfileRequestDto).GetProperty(nameof(CandidateProfileResponseDto.JobImportStatus)));
    }


    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutAsync_WhenRemotiveLimitIsNotPositive_ReturnsValidationProblem(int limit)
    {
        var candidateProfileService = new StubCandidateProfileService();
        var controller = new CandidateProfileController(
            candidateProfileService,
            new StubScheduledJobRunStatusService(new ScheduledJobRunStatus()),
            CreateConfiguration(enabled: true, intervalMinutes: 15));

        var result = await controller.PutAsync(new CandidateProfileRequestDto("Updated resume", null, null, limit), CancellationToken.None);

        var validation = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, validation.StatusCode);
        Assert.Null(candidateProfileService.SavedSettings);
    }

    private static IConfiguration CreateConfiguration(bool enabled, int intervalMinutes) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobImport:Enabled"] = enabled.ToString(),
                ["JobImport:IntervalMinutes"] = intervalMinutes.ToString()
            })
            .Build();

    private sealed class StubCandidateProfileService : ICandidateProfileService
    {
        private readonly CandidateProfileSettingsSnapshot settings;

        public StubCandidateProfileService(CandidateProfileSettingsSnapshot? settings = null)
        {
            this.settings = settings ?? new CandidateProfileSettingsSnapshot(string.Empty, null, null, null);
        }

        public CandidateProfileSettingsSnapshot? SavedSettings { get; private set; }

        public Task<CandidateProfileSettingsSnapshot> GetSettingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(settings);

        public Task SaveSettingsAsync(CandidateProfileSettingsSnapshot settings, CancellationToken cancellationToken = default)
        {
            SavedSettings = settings;
            return Task.CompletedTask;
        }

        public Task<string> GetResumeTextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(settings.ResumeText);

        public Task SaveResumeTextAsync(string resumeText, CancellationToken cancellationToken = default)
        {
            SavedSettings = settings with { ResumeText = resumeText };
            return Task.CompletedTask;
        }
    }

    private sealed class StubScheduledJobRunStatusService : IScheduledJobRunStatusService
    {
        private readonly ScheduledJobRunStatus status;

        public StubScheduledJobRunStatusService(ScheduledJobRunStatus status)
        {
            this.status = status;
        }

        public int GetCallCount { get; private set; }

        public Task<ScheduledJobRunStatus> GetAsync(CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            return Task.FromResult(status);
        }

        public Task MarkDisabledAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task MarkNoImporterConfiguredAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task MarkStartedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task MarkSucceededAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task MarkFailedAsync(Exception exception, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task MarkNextRunScheduledAsync(DateTimeOffset nextExpectedRunAt, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
