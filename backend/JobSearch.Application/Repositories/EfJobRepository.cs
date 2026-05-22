using JobSearch.Application.Abstractions;
using JobSearch.Application.Persistence;
using JobSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearch.Application.Repositories;

public sealed class EfJobRepository(AppDbContext dbContext) : IJobRepository
{
    public async Task<IReadOnlyCollection<JobOpportunity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
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

    public async Task UpdateAsync(JobOpportunity job, CancellationToken cancellationToken = default)
    {
        dbContext.Jobs.Update(job);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
