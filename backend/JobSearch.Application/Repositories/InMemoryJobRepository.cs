using System.Collections.Concurrent;
using JobSearch.Application.Abstractions;
using JobSearch.Domain.Entities;

namespace JobSearch.Application.Repositories;

public sealed class InMemoryJobRepository : IJobRepository
{
    private readonly ConcurrentDictionary<Guid, JobOpportunity> jobs = new();

    public Task<IReadOnlyCollection<JobOpportunity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<JobOpportunity> results = jobs.Values
            .OrderByDescending(job => job.DateFound)
            .ToArray();

        return Task.FromResult(results);
    }

    public Task<JobOpportunity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        jobs.TryGetValue(id, out var job);
        return Task.FromResult(job);
    }

    public Task<JobOpportunity> AddAsync(JobOpportunity job, CancellationToken cancellationToken = default)
    {
        jobs[job.Id] = job;
        return Task.FromResult(job);
    }

    public Task<JobOpportunity> UpsertImportedAsync(JobOpportunity job, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(job.Source))
        {
            throw new ArgumentException("Imported jobs must include a source.", nameof(job));
        }

        if (string.IsNullOrWhiteSpace(job.ExternalId) && string.IsNullOrWhiteSpace(job.Url))
        {
            throw new ArgumentException("Imported jobs must include an external id or URL.", nameof(job));
        }

        job.Source = job.Source.Trim();
        job.ExternalId = string.IsNullOrWhiteSpace(job.ExternalId) ? null : job.ExternalId.Trim();
        job.Url = string.IsNullOrWhiteSpace(job.Url) ? null : job.Url.Trim();

        var existing = jobs.Values.FirstOrDefault(existingJob => IsSameImportedJob(existingJob, job));
        if (existing is null)
        {
            jobs[job.Id] = job;
            return Task.FromResult(job);
        }

        UpdateImportedFields(existing, job);
        jobs[existing.Id] = existing;
        return Task.FromResult(existing);
    }

    public Task UpdateAsync(JobOpportunity job, CancellationToken cancellationToken = default)
    {
        jobs[job.Id] = job;
        return Task.CompletedTask;
    }

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

    private static void UpdateImportedFields(JobOpportunity existing, JobOpportunity imported)
    {
        existing.Company = imported.Company;
        existing.Title = imported.Title;
        existing.Location = imported.Location;
        existing.RemoteType = imported.RemoteType;
        existing.Url = imported.Url;
        existing.Source = imported.Source;
        existing.ExternalId = imported.ExternalId;
        existing.LastSeenAt = imported.LastSeenAt;
        existing.Description = imported.Description;
    }
}
