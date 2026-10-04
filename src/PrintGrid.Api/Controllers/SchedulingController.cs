using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Commands.AssignJob;
using PrintGrid.Modules.Scheduling.Application.Commands.RepairSchedule;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/scheduling")]
[Authorize(Policy = Policies.RequireOps)]
public class SchedulingController : ControllerBase
{
    private readonly ISender _sender;

    public SchedulingController(ISender sender) => _sender = sender;

    [HttpPost("jobs/{jobId:guid}/assign")]
    public async Task<IActionResult> AssignJob(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AssignJobCommand(jobId), cancellationToken);

        if (result.IsFailure)
            return Conflict(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        return Ok(result.Value);
    }

    [HttpPost("jobs/{jobId:guid}/repair-risk")]
    public async Task<IActionResult> RepairRisk(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RepairAtRiskJobCommand(jobId), cancellationToken);
        if (result.IsFailure) return Conflict(new { error = new { code = result.Error.Code, message = result.Error.Message } });
        return Ok(result.Value);
    }
}
