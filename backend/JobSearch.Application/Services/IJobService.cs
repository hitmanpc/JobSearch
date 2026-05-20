using JobSearch.Application.Dtos;

namespace JobSearch.Application.Services;

public interface IJobService
{
    Task<IReadOnlyCollection<JobOpportunityDto>> GetJobsAsync(CancellationToken cancellationToken = default);
    Task<JobOpportunityDto?> GetJobAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JobOpportunityDto> CreateJobAsync(CreateJobRequest request, CancellationToken cancellationToken = default);
    Task<JobOpportunityDto?> UpdateStatusAsync(Guid id, UpdateJobStatusRequest request, CancellationToken cancellationToken = default);
    Task<FitScoreResultDto?> ScoreFitAsync(Guid id, CancellationToken cancellationToken = default);
}
