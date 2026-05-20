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

    public Task UpdateAsync(JobOpportunity job, CancellationToken cancellationToken = default)
    {
        jobs[job.Id] = job;
        return Task.CompletedTask;
    }
}
