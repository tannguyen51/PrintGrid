using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Commands.AssignJob;
using PrintGrid.Modules.Scheduling.Application.Commands.RepairSchedule;
using PrintGrid.Modules.Scheduling.Application.Commands.UrgentReprint;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/scheduling/jobs/{jobId:guid}")]
[Authorize(Policy = Policies.RequireOps)]
public class SchedulingController(ISender sender) : ControllerBase
{
    [HttpPost("assign")]
    public async Task<IActionResult> AssignJob(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AssignJobCommand(jobId), cancellationToken);

        if (result.IsFailure)
            return Conflict(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        return Ok(result.Value);
    }

    /// <summary>
    /// POST /scheduling/jobs/{jobId}/reprint — manual trigger for the URGENT reprint of a
    /// failed job (FR-HUB-003). The hub QC failure path raises the same command automatically.
    /// </summary>
    [HttpPost("reprint")]
    public async Task<IActionResult> CreateUrgentReprint(
        Guid jobId,
        [FromBody] CreateUrgentReprintRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateUrgentReprintCommand(jobId, request.Reason, request.FaultLabId),
            cancellationToken);

        if (!result.IsFailure) return Ok(result.Value);
        var status = result.Error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });

    }

    /// <summary>
    /// POST /scheduling/jobs/{jobId}/repair-risk — split an at-risk job across labs to protect
    /// its committed date (BR-SCHED-010, the first step of the FR-SCHED-007 repair order).
    /// </summary>
    [HttpPost("repair-risk")]
    public async Task<IActionResult> RepairRisk(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RepairAtRiskJobCommand(jobId), cancellationToken);
        if (result.IsFailure) return Conflict(new { error = new { code = result.Error.Code, message = result.Error.Message } });
        return Ok(result.Value);
    }
}

public record CreateUrgentReprintRequest(string Reason, Guid? FaultLabId = null);
