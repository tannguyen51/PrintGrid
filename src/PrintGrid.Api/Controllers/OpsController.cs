using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Queries;

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
}
