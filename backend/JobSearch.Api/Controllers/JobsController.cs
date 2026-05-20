using JobSearch.Application.Dtos;
using JobSearch.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobSearch.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly IJobService jobService;

    public JobsController(IJobService jobService)
    {
        this.jobService = jobService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<JobOpportunityDto>>> GetJobs(CancellationToken cancellationToken)
    {
        var jobs = await jobService.GetJobsAsync(cancellationToken);
        return Ok(jobs);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobOpportunityDto>> GetJob(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobService.GetJobAsync(id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost]
    public async Task<ActionResult<JobOpportunityDto>> CreateJob(
        CreateJobRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var job = await jobService.CreateJobAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<JobOpportunityDto>> UpdateStatus(
        Guid id,
        UpdateJobStatusRequest request,
        CancellationToken cancellationToken)
    {
        var job = await jobService.UpdateStatusAsync(id, request, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost("{id:guid}/score")]
    public async Task<ActionResult<FitScoreResultDto>> ScoreFit(Guid id, CancellationToken cancellationToken)
    {
        var scoreResult = await jobService.ScoreFitAsync(id, cancellationToken);
        return scoreResult is null ? NotFound() : Ok(scoreResult);
    }
}
