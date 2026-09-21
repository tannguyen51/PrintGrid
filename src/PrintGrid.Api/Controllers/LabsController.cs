using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Queries;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/labs")]
[Authorize(Policy = Policies.RequireOps)]
public class LabsController : ControllerBase
{
    private readonly ISender _sender;

    public LabsController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> GetLabs([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetLabsQuery(includeInactive), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpGet("{labId:guid}")]
    public async Task<IActionResult> GetLab(Guid labId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLabQuery(labId), cancellationToken);
        return result.IsFailure
            ? NotFound(new { error = new { code = result.Error.Code, message = result.Error.Message } })
            : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> RegisterLab([FromBody] RegisterLabRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterLabCommand(request.Name, request.City, request.TransitDaysToHub),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        return CreatedAtAction(nameof(GetLab), new { labId = result.Value.Id }, result.Value);
    }

    [HttpPost("{labId:guid}/machines")]
    public async Task<IActionResult> RegisterMachine(Guid labId, [FromBody] RegisterMachineRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterMachineCommand(
                labId,
                request.Name,
                request.Model,
                request.Technology,
                request.BuildWidthMm,
                request.BuildDepthMm,
                request.BuildHeightMm,
                request.MinLayerHeightMm,
                request.AchievableToleranceMm,
                request.SupportedMaterials),
            cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code == "not_found" ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        return CreatedAtAction(nameof(GetLab), new { labId }, result.Value);
    }

    [HttpPatch("{labId:guid}/active")]
    public async Task<IActionResult> SetActive(Guid labId, [FromBody] SetActiveRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetLabActiveCommand(labId, request.IsActive), cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        return NoContent();
    }

    [HttpPatch("{labId:guid}/machines/{machineId:guid}/status")]
    public async Task<IActionResult> SetMachineStatus(Guid labId, Guid machineId, [FromBody] SetMachineStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetMachineStatusCommand(labId, machineId, request.Status), cancellationToken);
        if (result.IsFailure)
        {
            var status = result.Error.Code == "not_found" ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        return NoContent();
    }
}

public record RegisterLabRequest(string Name, string City, int TransitDaysToHub);

public record RegisterMachineRequest(
    string Name,
    string Model,
    PrintTechnology Technology,
    decimal BuildWidthMm,
    decimal BuildDepthMm,
    decimal BuildHeightMm,
    decimal MinLayerHeightMm,
    decimal AchievableToleranceMm,
    IReadOnlyList<string> SupportedMaterials);

public record SetActiveRequest(bool IsActive);

public record SetMachineStatusRequest(MachineStatus Status);