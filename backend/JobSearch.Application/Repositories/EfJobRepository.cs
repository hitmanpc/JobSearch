using JobSearch.Application.Abstractions;
using JobSearch.Application.Persistence;
using JobSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearch.Application.Repositories;

public sealed class EfJobRepository(AppDbContext dbContext) : IJobRepository
{
    public async Task<IReadOnlyCollection<JobOpportunity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var all = await dbContext.Jobs.AsNoTracking().ToArrayAsync(cancellationToken);
            return all.OrderByDescending(x => x.DateFound).ToArray();
        }

        return await dbContext.Jobs.AsNoTracking()
            .OrderByDescending(x => x.DateFound)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<JobOpportunity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Jobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<JobOpportunity> AddAsync(JobOpportunity job, CancellationToken cancellationToken = default)
    {
        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<JobOpportunity> UpsertImportedAsync(JobOpportunity job, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(job.Source))
        {
            throw new ArgumentException("Imported jobs must include a source.", nameof(job));
        }

        if (string.IsNullOrWhiteSpace(job.ExternalId) && string.IsNullOrWhiteSpace(job.Url))
        {
            throw new ArgumentException("Imported jobs must include an external id or URL.", nameof(job));
        }

        var source = job.Source.Trim();
        var externalId = string.IsNullOrWhiteSpace(job.ExternalId) ? null : job.ExternalId.Trim();
        var url = string.IsNullOrWhiteSpace(job.Url) ? null : job.Url.Trim();

        var existing = externalId is not null
            ? await dbContext.Jobs.FirstOrDefaultAsync(
                existingJob => existingJob.Source == source && existingJob.ExternalId == externalId,
                cancellationToken)
            : await dbContext.Jobs.FirstOrDefaultAsync(
                existingJob => existingJob.Source == source && existingJob.ExternalId == null && existingJob.Url == url,
                cancellationToken);

        job.Source = source;
        job.ExternalId = externalId;
        job.Url = url;

        if (existing is null)
        {
            dbContext.Jobs.Add(job);
            await dbContext.SaveChangesAsync(cancellationToken);
            return job;
        }

        UpdateImportedFields(existing, job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task UpdateAsync(JobOpportunity job, CancellationToken cancellationToken = default)
    {
        dbContext.Jobs.Update(job);
        await dbContext.SaveChangesAsync(cancellationToken);
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
