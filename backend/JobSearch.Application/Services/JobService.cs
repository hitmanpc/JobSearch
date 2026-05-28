using JobSearch.Application.Abstractions;
using JobSearch.Application.Dtos;
using JobSearch.Domain.Entities;
using JobSearch.Domain.Enums;

namespace JobSearch.Application.Services;

public sealed class JobService : IJobService
{
    private readonly IJobRepository repository;
    private readonly IFitScoringService fitScoringService;
    private readonly ICandidateProfileService candidateProfileService;
    private readonly TimeProvider timeProvider;

    public JobService(
        IJobRepository repository,
        IFitScoringService fitScoringService,
        ICandidateProfileService candidateProfileService,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.fitScoringService = fitScoringService;
        this.candidateProfileService = candidateProfileService;
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

    public async Task<FitScoreResultDto?> ScoreFitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await repository.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var resumeText = await candidateProfileService.GetResumeTextAsync(cancellationToken);
        var scoreResult = await fitScoringService.ScoreAsync(job, resumeText, cancellationToken);
        job.FitScore = scoreResult.FitScore;
        job.FitScoreResult = scoreResult;

        await repository.UpdateAsync(job, cancellationToken);
        return FitScoreResultDto.FromEntity(scoreResult);
    }

    public async Task<GeneratedRecruiterMessageDto?> GenerateRecruiterMessageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await repository.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var matchingSkills = job.FitScoreResult?.MatchingSkills
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => skill.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray()
            ?? Array.Empty<string>();

        var skillsText = matchingSkills.Length > 0
            ? string.Join(", ", matchingSkills)
            : "Angular, .NET, cloud architecture, and API design";

        var message =
            $"Hi there,\n\n" +
            $"I'm excited about the {job.Title} opportunity at {job.Company}. " +
            $"My background aligns well with the role, especially in {skillsText}. " +
            "I enjoy building reliable products, collaborating across teams, and driving outcomes from idea to production.\n\n" +
            "You can review my portfolio at donbowman.info. " +
            "If helpful, I'd love to connect and briefly discuss how I could contribute.\n\n" +
            "Best regards,";

        job.GeneratedRecruiterMessage = message;
        await repository.UpdateAsync(job, cancellationToken);

        return new GeneratedRecruiterMessageDto(message);
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
