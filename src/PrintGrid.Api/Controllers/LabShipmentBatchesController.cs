using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Commands.ShipmentBatches;
using PrintGrid.Modules.Scheduling.Application.Queries;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/lab/shipment-batches")]
[Authorize(Policy = Policies.RequireLab)]
public sealed class LabShipmentBatchesController(ISender sender) : ControllerBase
{
    [HttpGet("planner")]
    public async Task<IActionResult> Planner([FromQuery] Guid labId, CancellationToken ct)
    {
        if (await ForbidIfNotCallersLabAsync(labId, ct) is { } forbidden) return forbidden;
        return ToActionResult(await sender.Send(new GetShipmentPlannerQuery(labId), ct));
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] ShipmentSelectionRequest request, CancellationToken ct)
    {
        if (await ForbidIfNotCallersLabAsync(request.LabId, ct) is { } forbidden) return forbidden;
        return ToActionResult(await sender.Send(new PreviewShipmentBatchQuery(request.LabId, request.JobIds), ct));
    }

    [HttpPost("seal")]
    public async Task<IActionResult> Seal([FromBody] ShipmentSelectionRequest request, CancellationToken ct)
    {
        if (await ForbidIfNotCallersLabAsync(request.LabId, ct) is { } forbidden) return forbidden;
        var staffId = User.GetCustomerId();
        if (staffId is null) return Forbid();
        return ToActionResult(await sender.Send(new SealShipmentBatchCommand(request.LabId, request.JobIds, staffId.Value), ct));
    }

    /// <summary>
    /// BR-ACCESS-003/004: a lab batch may only be planned/sealed for the caller's own lab.
    /// Lab operators resolve through the same owned-or-unclaimed lab lookup as job actions.
    /// </summary>
    private async Task<IActionResult?> ForbidIfNotCallersLabAsync(Guid labId, CancellationToken ct)
    {
        var userId = User.GetCustomerId();
        if (userId is null) return Forbid();

        var mine = await sender.Send(new GetMyLabQuery(userId.Value), ct);
        if (mine.IsFailure || mine.Value is null || mine.Value.Id != labId) return Forbid();
        return null;
    }

    private IActionResult ToActionResult<T>(Result<T> result)
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

public sealed record ShipmentSelectionRequest(Guid LabId, IReadOnlyList<Guid> JobIds);
