using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Commands.ShipmentBatches;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// FR-HUB-001: the hub side of the lab→hub batch handover. A sealed batch sits in
/// <c>Sealed</c> with every job <c>InTransit</c>; receiving it moves the jobs to
/// <c>AwaitingInspection</c> so QC can start. Without this endpoint a sealed batch was a
/// dead end — no caller ever invoked <c>Job.ReceiveAtHub</c>.
/// </summary>
[ApiController]
[Route("api/v1/hub/shipment-batches")]
[Authorize(Policy = Policies.RequireHub)]
public sealed class HubShipmentBatchesController(ISender sender, IShipmentBatchRepository batches) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSealed(CancellationToken ct)
    {
        var sealedBatches = await batches.GetByStatusAsync(ShipmentBatchStatus.Sealed, ct);
        return Ok(sealedBatches.Select(batch => new ShipmentBatchDto(
            batch.Id,
            batch.LabId,
            batch.WaybillNumber,
            batch.Status.ToString(),
            batch.JobCount,
            batch.TotalWeightGrams,
            batch.TransportZone,
            batch.ScheduledTransferDate,
            batch.SealedAtUtc)));
    }

    [HttpPost("{batchId:guid}/receive")]
    public async Task<IActionResult> Receive(
        Guid batchId,
        [FromBody] ReceiveShipmentBatchRequest? request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new ReceiveShipmentBatchCommand(batchId, User.GetCustomerId(), request?.ScannedJobIds),
            ct);

        if (result.IsFailure)
        {
            var status = result.Error.Code switch
            {
                "not_found" => StatusCodes.Status404NotFound,
                "conflict" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        return Ok(result.Value);
    }
}

public sealed record ReceiveShipmentBatchRequest(IReadOnlyList<Guid>? ScannedJobIds = null);

public sealed record ShipmentBatchDto(
    Guid Id,
    Guid LabId,
    string WaybillNumber,
    string Status,
    int JobCount,
    decimal TotalWeightGrams,
    string TransportZone,
    DateOnly ScheduledTransferDate,
    DateTime SealedAtUtc);
