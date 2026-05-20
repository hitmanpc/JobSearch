using JobSearch.Domain.Entities;
using JobSearch.Domain.Enums;

namespace JobSearch.Application.Dtos;

public sealed record JobOpportunityDto(
    Guid Id,
    string Company,
    string Title,
    string? Location,
    RemoteType RemoteType,
    string? Url,
    string? Description,
    ApplicationStatus Status,
    int? FitScore,
    FitScoreResultDto? FitScoreResult,
    DateTimeOffset DateFound,
    DateTimeOffset? DateApplied)
{
    public static JobOpportunityDto FromEntity(JobOpportunity job) =>
        new(
            job.Id,
            job.Company,
            job.Title,
            job.Location,
            job.RemoteType,
            job.Url,
            job.Description,
            job.Status,
            job.FitScore,
            job.FitScoreResult is null ? null : FitScoreResultDto.FromEntity(job.FitScoreResult),
            job.DateFound,
            job.DateApplied);
}
