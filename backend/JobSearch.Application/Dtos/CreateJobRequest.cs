using JobSearch.Domain.Enums;

namespace JobSearch.Application.Dtos;

public sealed record CreateJobRequest(
    string Company,
    string Title,
    string? Location,
    RemoteType RemoteType,
    string? Url,
    string? Description,
    int? FitScore);
