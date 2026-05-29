using JobSearch.Application.Automation;
using JobSearch.Application.Dtos;
using JobSearch.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobSearch.Api.Controllers;

[ApiController]
[Route("api/candidate-profile")]
public sealed class CandidateProfileController(
    ICandidateProfileService candidateProfileService,
    IScheduledJobRunStatusService scheduledJobRunStatusService,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<CandidateProfileResponseDto> GetAsync(CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.GetSettingsAsync(cancellationToken);
        var jobImportStatus = await scheduledJobRunStatusService.GetAsync(cancellationToken);
        var interval = ScheduledJobImportWorker.GetInterval(configuration);

        return new CandidateProfileResponseDto(
            profile.ResumeText,
            profile.RemotiveCategory,
            profile.RemotiveSearchText,
            profile.RemotiveLimit,
            new JobImportStatusDto(
                ScheduledJobImportWorker.IsEnabled(configuration),
                (int)interval.TotalMinutes,
                jobImportStatus.LastRunStartedAt,
                jobImportStatus.LastRunCompletedAt,
                jobImportStatus.LastRunSucceeded,
                jobImportStatus.LastResult,
                jobImportStatus.LastErrorMessage,
                jobImportStatus.NextExpectedRunAt));
    }

    [HttpPut]
    public async Task<IActionResult> PutAsync([FromBody] CandidateProfileRequestDto dto, CancellationToken cancellationToken)
    {
        if (dto.RemotiveLimit is <= 0)
        {
            ModelState.AddModelError(nameof(dto.RemotiveLimit), "Remotive limit must be a positive integer when provided.");
            return ValidationProblem(ModelState);
        }

        await candidateProfileService.SaveSettingsAsync(
            new CandidateProfileSettingsSnapshot(
                dto.ResumeText ?? string.Empty,
                dto.RemotiveCategory,
                dto.RemotiveSearchText,
                dto.RemotiveLimit),
            cancellationToken);
        return NoContent();
    }
}
