using JobSearch.Application.Abstractions;
using JobSearch.Application.Dtos;
using JobSearch.Domain.Entities;
using JobSearch.Domain.Enums;

namespace JobSearch.Application.Services;

public sealed class JobService : IJobService
{
    private readonly IJobRepository repository;
    private readonly TimeProvider timeProvider;

    public JobService(IJobRepository repository, TimeProvider timeProvider)
    {
        this.repository = repository;
        this.timeProvider = timeProvider;
    }

    public async Task<IReadOnlyCollection<JobOpportunityDto>> GetJobsAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await repository.GetAllAsync(cancellationToken);
        return jobs.Select(JobOpportunityDto.FromEntity).ToArray();
    }

    public async Task<JobOpportunityDto?> GetJobAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await repository.GetByIdAsync(id, cancellationToken);
        return job is null ? null : JobOpportunityDto.FromEntity(job);
    }

    public async Task<JobOpportunityDto> CreateJobAsync(CreateJobRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);

        var job = new JobOpportunity
        {
            Company = request.Company.Trim(),
            Title = request.Title.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
            RemoteType = request.RemoteType,
            Url = string.IsNullOrWhiteSpace(request.Url) ? null : request.Url.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            FitScore = request.FitScore,
            DateFound = timeProvider.GetUtcNow()
        };

        var created = await repository.AddAsync(job, cancellationToken);
        return JobOpportunityDto.FromEntity(created);
    }

    public async Task<JobOpportunityDto?> UpdateStatusAsync(Guid id, UpdateJobStatusRequest request, CancellationToken cancellationToken = default)
    {
        var job = await repository.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        job.Status = request.Status;
        job.DateApplied = request.Status == ApplicationStatus.Applied
            ? timeProvider.GetUtcNow()
            : job.DateApplied;

        await repository.UpdateAsync(job, cancellationToken);
        return JobOpportunityDto.FromEntity(job);
    }

    private static void ValidateCreateRequest(CreateJobRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Company))
        {
            throw new ArgumentException("Company is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title is required.", nameof(request));
        }

        if (request.FitScore is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "FitScore must be between 0 and 100.");
        }
    }
}
