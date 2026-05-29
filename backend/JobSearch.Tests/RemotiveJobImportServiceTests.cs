using System.Net;
using JobSearch.Application.Abstractions;
using JobSearch.Application.Services;
using JobSearch.Domain.Entities;
using JobSearch.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JobSearch.Tests;

public sealed class RemotiveJobImportServiceTests
{
    [Fact]
    public async Task ImportAsync_UsesPublicRemotiveJobsEndpointAndMapsJobsWithoutApplyingOrMessaging()
    {
        var repository = new CapturingJobRepository();
        var handler = new JsonResponseHandler("""
            {
              "jobs": [
                {
                  "id": 123,
                  "url": "https://remotive.com/remote-jobs/software-dev/senior-full-stack-engineer-123",
                  "title": "Senior Full Stack Engineer",
                  "company_name": "Example Co",
                  "candidate_required_location": "Worldwide",
                  "job_type": "full_time",
                  "publication_date": "2026-05-20T12:30:00Z",
                  "category": "Software Development",
                  "tags": ["c#", "angular"],
                  "salary": "$120k-$160k",
                  "description": "<p>Build APIs &amp; Angular apps.</p>"
                }
              ]
            }
            """);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://remotive.com")
        };
        var service = new RemotiveJobImportService(
            httpClient,
            repository,
            TimeProvider.System,
            NullLogger<RemotiveJobImportService>.Instance);

        await service.ImportAsync();

        Assert.Equal(new Uri("https://remotive.com/api/remote-jobs"), handler.RequestUri);
        var job = Assert.Single(repository.AddedJobs);
        Assert.Equal("Example Co", job.Company);
        Assert.Equal("Senior Full Stack Engineer", job.Title);
        Assert.Equal("Worldwide", job.Location);
        Assert.Equal(RemoteType.Remote, job.RemoteType);
        Assert.Equal("https://remotive.com/remote-jobs/software-dev/senior-full-stack-engineer-123", job.Url);
        Assert.Equal("Remotive", job.Source);
        Assert.Equal("123", job.ExternalId);
        Assert.NotNull(job.LastSeenAt);
        Assert.Equal(ApplicationStatus.Found, job.Status);
        Assert.Null(job.DateApplied);
        Assert.Null(job.GeneratedRecruiterMessage);
        Assert.NotNull(job.Description);
        Assert.Contains("Build APIs & Angular apps.", job.Description);
    }

    [Fact]
    public async Task ImportAsync_UpsertsDuplicateRemotiveUrlsWithoutExternalIds()
    {
        const string duplicateUrl = "https://remotive.com/remote-jobs/software-dev/existing";
        var repository = new CapturingJobRepository(new JobOpportunity
        {
            Company = "Existing Co",
            Title = "Existing Role",
            Url = duplicateUrl,
            Source = "Remotive"
        });
        var handler = new JsonResponseHandler($$"""
            {
              "jobs": [
                {
                  "url": "{{duplicateUrl}}",
                  "title": "Updated Existing Role",
                  "company_name": "Updated Existing Co"
                },
                {
                  "id": 2,
                  "url": "https://remotive.com/remote-jobs/software-dev/new",
                  "title": "New Role",
                  "company_name": "New Co"
                }
              ]
            }
            """);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://remotive.com")
        };
        var service = new RemotiveJobImportService(
            httpClient,
            repository,
            TimeProvider.System,
            NullLogger<RemotiveJobImportService>.Instance);

        await service.ImportAsync();

        var job = Assert.Single(repository.AddedJobs);
        Assert.Equal("New Co", job.Company);
        Assert.Equal("https://remotive.com/remote-jobs/software-dev/new", job.Url);

        var updatedJob = Assert.Single(repository.UpdatedJobs);
        Assert.Equal("Updated Existing Co", updatedJob.Company);
        Assert.Equal("Updated Existing Role", updatedJob.Title);
        Assert.Equal(duplicateUrl, updatedJob.Url);
    }

    private sealed class JsonResponseHandler : HttpMessageHandler
    {
        private readonly string json;

        public JsonResponseHandler(string json)
        {
            this.json = json;
        }

        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class CapturingJobRepository : IJobRepository
    {
        private readonly List<JobOpportunity> existingJobs;

        public CapturingJobRepository(params JobOpportunity[] existingJobs)
        {
            this.existingJobs = existingJobs.ToList();
        }

        public List<JobOpportunity> AddedJobs { get; } = new();
        public List<JobOpportunity> UpdatedJobs { get; } = new();

        public Task<IReadOnlyCollection<JobOpportunity>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<JobOpportunity>>(existingJobs.Concat(AddedJobs).ToArray());

        public Task<JobOpportunity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(existingJobs.Concat(AddedJobs).SingleOrDefault(job => job.Id == id));

        public Task<JobOpportunity> AddAsync(JobOpportunity job, CancellationToken cancellationToken = default)
        {
            AddedJobs.Add(job);
            return Task.FromResult(job);
        }

        public Task<JobOpportunity> UpsertImportedAsync(JobOpportunity job, CancellationToken cancellationToken = default)
        {
            var existing = existingJobs.Concat(AddedJobs).FirstOrDefault(existingJob => IsSameImportedJob(existingJob, job));
            if (existing is null)
            {
                AddedJobs.Add(job);
                return Task.FromResult(job);
            }

            existing.Company = job.Company;
            existing.Title = job.Title;
            existing.Location = job.Location;
            existing.RemoteType = job.RemoteType;
            existing.Url = job.Url;
            existing.Source = job.Source;
            existing.ExternalId = job.ExternalId;
            existing.LastSeenAt = job.LastSeenAt;
            existing.Description = job.Description;
            UpdatedJobs.Add(existing);
            return Task.FromResult(existing);
        }

        public Task UpdateAsync(JobOpportunity job, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        private static bool IsSameImportedJob(JobOpportunity existingJob, JobOpportunity importedJob)
        {
            if (!string.Equals(existingJob.Source, importedJob.Source, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(importedJob.ExternalId))
            {
                return string.Equals(existingJob.ExternalId, importedJob.ExternalId, StringComparison.Ordinal);
            }

            return string.IsNullOrWhiteSpace(existingJob.ExternalId) &&
                string.Equals(existingJob.Url, importedJob.Url, StringComparison.Ordinal);
        }
    }
}
