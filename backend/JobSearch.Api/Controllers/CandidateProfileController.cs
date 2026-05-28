using JobSearch.Application.Dtos;
using JobSearch.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobSearch.Api.Controllers;

[ApiController]
[Route("api/candidate-profile")]
public sealed class CandidateProfileController(ICandidateProfileService candidateProfileService) : ControllerBase
{
    [HttpGet]
    public async Task<CandidateProfileDto> GetAsync(CancellationToken cancellationToken)
    {
        var resumeText = await candidateProfileService.GetResumeTextAsync(cancellationToken);
        return new CandidateProfileDto(resumeText);
    }

    [HttpPut]
    public async Task<IActionResult> PutAsync([FromBody] CandidateProfileDto dto, CancellationToken cancellationToken)
    {
        await candidateProfileService.SaveResumeTextAsync(dto.ResumeText ?? string.Empty, cancellationToken);
        return NoContent();
    }
}
