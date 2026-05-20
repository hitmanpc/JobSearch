using JobSearch.Domain.Enums;

namespace JobSearch.Application.Dtos;

public sealed record UpdateJobStatusRequest(ApplicationStatus Status);
