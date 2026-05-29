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
        var resumeText = await candidateProfileService.GetResumeTextAsync(cancellationToken);
        var jobImportStatus = await scheduledJobRunStatusService.GetAsync(cancellationToken);
        var interval = ScheduledJobImportWorker.GetInterval(configuration);

        return new CandidateProfileResponseDto(
            resumeText,
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
        await candidateProfileService.SaveResumeTextAsync(dto.ResumeText ?? string.Empty, cancellationToken);
        return NoContent();
    }
}
