using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Commands.ShipmentBatches;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/lab/shipment-batches")]
[Authorize(Policy = Policies.RequireLab)]
public sealed class LabShipmentBatchesController(ISender sender) : ControllerBase
{
    [HttpGet("planner")]
    public async Task<IActionResult> Planner([FromQuery] Guid labId, CancellationToken ct) =>
        ToActionResult(await sender.Send(new GetShipmentPlannerQuery(labId), ct));

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] ShipmentSelectionRequest request, CancellationToken ct) =>
        ToActionResult(await sender.Send(new PreviewShipmentBatchQuery(request.LabId, request.JobIds), ct));

    [HttpPost("seal")]
    public async Task<IActionResult> Seal([FromBody] ShipmentSelectionRequest request, CancellationToken ct)
    {
        var staffId = User.GetCustomerId();
        if (staffId is null) return Forbid();
        return ToActionResult(await sender.Send(new SealShipmentBatchCommand(request.LabId, request.JobIds, staffId.Value), ct));
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
