using JobSearch.Domain.Entities;

namespace JobSearch.Application.Abstractions;

public interface IJobRepository
{
    Task<IReadOnlyCollection<JobOpportunity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<JobOpportunity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JobOpportunity> AddAsync(JobOpportunity job, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobOpportunity job, CancellationToken cancellationToken = default);
}
