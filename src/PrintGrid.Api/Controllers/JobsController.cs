using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;
using PrintGrid.Modules.Scheduling.Application.Queries;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly ISender _sender;

    public JobsController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize(Policy = Policies.RequireProduction)]
    // Lab/Hub/Ops each read the job board — they filter by status in the UI.
    public async Task<IActionResult> GetJobs([FromQuery] JobStatus? status, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetJobsQuery(status ?? JobStatus.Pending), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpPost("{jobId:guid}/accept")]
    [Authorize(Policy = Policies.RequireLab)]
    public async Task<IActionResult> Accept(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AcceptJobCommand(jobId), cancellationToken);
        return ToResult(result);
    }

    [HttpPost("{jobId:guid}/decline")]
    [Authorize(Policy = Policies.RequireLab)]
    public async Task<IActionResult> Decline(Guid jobId, [FromBody] DeclineJobRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeclineJobCommand(jobId, request.Reason), cancellationToken);
        return ToResult(result);
    }

    [HttpPost("{jobId:guid}/start")]
    [Authorize(Policy = Policies.RequireLab)]
    public async Task<IActionResult> Start(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new StartJobCommand(jobId), cancellationToken);
        return ToResult(result);
    }

    [HttpPost("{jobId:guid}/complete")]
    [Authorize(Policy = Policies.RequireLab)]
    public async Task<IActionResult> Complete(Guid jobId, [FromBody] CompleteJobRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CompleteJobCommand(jobId, request.ActualPrintMinutes), cancellationToken);
        return ToResult(result);
    }

    [HttpPost("{jobId:guid}/inspect")]
    [Authorize(Policy = Policies.RequireHub)]
    public async Task<IActionResult> Inspect(Guid jobId, [FromBody] InspectRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new InspectJobCommand(
            jobId,
            request.Passed,
            request.ChecklistResults ?? Array.Empty<ChecklistItemResult>(),
            request.PhotoUrls ?? Array.Empty<string>(),
            request.FaultAttribution,
            request.Note), cancellationToken);
        return ToResult(result);
    }

    private IActionResult ToResult(Result result)
    {
        if (result.IsSuccess) return NoContent();
        var status = result.Error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
    }
}

public record CompleteJobRequest(int ActualPrintMinutes);
public record InspectRequest(
    bool Passed,
    IReadOnlyList<ChecklistItemResult>? ChecklistResults = null,
    IReadOnlyList<string>? PhotoUrls = null,
    FaultAttribution? FaultAttribution = null,
    string? Note = null);
public record DeclineJobRequest(string Reason);
