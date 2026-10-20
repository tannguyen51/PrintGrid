using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Queries;
using PrintGrid.Modules.Scheduling.Application.Commands.OpsWorkflow;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// Operations console endpoints. The decision trace is the audit surface FR-SCHED-009 /
/// NFR-LEGAL-002 require: read-only, and deliberately without any update or delete route.
/// </summary>
[ApiController]
[Route("api/v1/ops")]
[Authorize(Policy = Policies.RequireOps)]
public class OpsController : ControllerBase
{
    private readonly ISender _sender;

    public OpsController(ISender sender) => _sender = sender;

    /// <summary>GET /ops/decisions/{jobId} — candidates, per-criterion scores, config version.</summary>
    [HttpGet("decisions/{jobId:guid}")]
    public async Task<IActionResult> GetDecisionTrace(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDecisionLogQuery(jobId), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        return Ok(result.Value);
    }

    /// <summary>GET /ops/escalations — reprints past the cap and jobs with no feasible date.</summary>
    [HttpGet("escalations")]
    public async Task<IActionResult> GetEscalations(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOpsEscalationsQuery(), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        return Ok(result.Value);
    }

    [HttpGet("monitor")]
    public async Task<IActionResult> GetMonitor(CancellationToken cancellationToken) =>
        ToResult(await _sender.Send(new GetOpsMonitorQuery(), cancellationToken));

    [HttpGet("jobs/{jobId:guid}/reassign-preview")]
    public async Task<IActionResult> PreviewReassign(Guid jobId, CancellationToken cancellationToken) =>
        ToResult(await _sender.Send(new PreviewOpsReassignQuery(jobId), cancellationToken));

    [HttpPost("jobs/{jobId:guid}/reassign")]
    public async Task<IActionResult> Reassign(Guid jobId, [FromBody] OpsReassignRequest request, CancellationToken cancellationToken)
    {
        var actorId = User.GetCustomerId();
        if (actorId is null) return Forbid();
        PrintGrid.Modules.Scheduling.Domain.Enums.DateChangeReason? reasonCode = null;
        if (Enum.TryParse<PrintGrid.Modules.Scheduling.Domain.Enums.DateChangeReason>(request.DateChangeReason, true, out var parsed))
            reasonCode = parsed;
        return ToResult(await _sender.Send(new ReassignJobByOpsCommand(jobId, actorId.Value, request.Reason, request.ConfirmCustomerDateImpact, reasonCode), cancellationToken));
    }

    private IActionResult ToResult<T>(PrintGrid.SharedKernel.Results.Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        var status = result.Error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
    }
}

public sealed record OpsReassignRequest(string Reason, bool ConfirmCustomerDateImpact = false, string? DateChangeReason = null);
