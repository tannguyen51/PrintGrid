using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;
using PrintGrid.Modules.Scheduling.Application.Queries;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Admin.Application.AuditLogs;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/labs")]
[Authorize]
public class LabsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuditLogWriter _audit;

    public LabsController(ISender sender, IAuditLogWriter audit)
    {
        _sender = sender;
        _audit = audit;
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.LabManager},{Roles.OpsManager},{Roles.Admin}")]
    public async Task<IActionResult> GetLabs([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var ownerUserId = User.IsInRole(Roles.LabManager) ? User.GetCustomerId() : null;
        if (User.IsInRole(Roles.LabManager) && ownerUserId is null) return Forbid();

        var result = await _sender.Send(new GetLabsQuery(includeInactive, ownerUserId), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpGet("{labId:guid}")]
    [Authorize(Roles = $"{Roles.LabManager},{Roles.OpsManager},{Roles.Admin}")]
    public async Task<IActionResult> GetLab(Guid labId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLabQuery(labId), cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = new { code = result.Error.Code, message = result.Error.Message } });
        if (User.IsInRole(Roles.LabManager) && result.Value.OwnerUserId != User.GetCustomerId()) return Forbid();
        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Roles = Roles.LabManager)]
    public async Task<IActionResult> RegisterLab([FromBody] RegisterLabRequest request, CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetCustomerId();
        if (ownerUserId is null) return Forbid();
        var result = await _sender.Send(
            new RegisterLabCommand(request.Name, request.City, request.TransitDaysToHub, ownerUserId,
                request.WorkingDays, request.OpeningTime, request.ClosingTime),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        await WriteAudit("lab.registration_submitted", "Lab", result.Value.Id, null, result.Value,
            ["FR-LAB-001", "BR-OPS-005", "BR-OPS-006", "UC-014"], null, cancellationToken);

        return CreatedAtAction(nameof(GetLab), new { labId = result.Value.Id }, result.Value);
    }

    [HttpPost("{labId:guid}/machines")]
    [Authorize(Roles = Roles.LabManager)]
    public async Task<IActionResult> RegisterMachine(Guid labId, [FromBody] RegisterMachineRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = User.GetCustomerId();
        if (actorUserId is null) return Forbid();
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
                request.SupportedMaterials,
                actorUserId),
            cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code switch
            {
                "not_found" => StatusCodes.Status404NotFound,
                "forbidden" => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        await WriteAudit("machine.registered", "Machine", result.Value.Id, null, result.Value,
            ["FR-LAB-002", "BR-OPS-006", "UC-014"], null, cancellationToken);

        return CreatedAtAction(nameof(GetLab), new { labId }, result.Value);
    }

    [HttpPatch("{labId:guid}/active")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> SetActive(Guid labId, [FromBody] SetActiveRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetLabActiveCommand(labId, request.IsActive), cancellationToken);
        if (result.IsFailure)
        {
            var status = result.Error.Code == "not_found" ? StatusCodes.Status404NotFound : StatusCodes.Status409Conflict;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        await WriteAudit(request.IsActive ? "lab.approved" : "lab.deactivated", "Lab", labId, null,
            new { request.IsActive }, ["FR-LAB-001", "BR-OPS-005", "BR-OPS-006", "UC-014"], null, cancellationToken);

        return NoContent();
    }

    [HttpPatch("{labId:guid}/machines/{machineId:guid}/status")]
    [Authorize(Roles = Roles.LabManager)]
    public async Task<IActionResult> SetMachineStatus(Guid labId, Guid machineId, [FromBody] SetMachineStatusRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = User.GetCustomerId();
        if (actorUserId is null) return Forbid();
        var result = await _sender.Send(new SetMachineStatusCommand(labId, machineId, request.Status, actorUserId), cancellationToken);
        if (result.IsFailure)
        {
            var status = result.Error.Code switch
            {
                "not_found" => StatusCodes.Status404NotFound,
                "forbidden" => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        return NoContent();
    }

    [HttpPut("{labId:guid}/machines/{machineId:guid}")]
    [Authorize(Roles = Roles.LabManager)]
    public async Task<IActionResult> UpdateMachine(Guid labId, Guid machineId, [FromBody] RegisterMachineRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = User.GetCustomerId();
        if (actorUserId is null) return Forbid();
        var result = await _sender.Send(new UpdateMachineCommand(
            labId, machineId, request.Name, request.Model, request.Technology,
            request.BuildWidthMm, request.BuildDepthMm, request.BuildHeightMm,
            request.MinLayerHeightMm, request.AchievableToleranceMm,
            request.SupportedMaterials, actorUserId.Value), cancellationToken);
        if (result.IsFailure)
        {
            var status = result.Error.Code switch
            {
                "not_found" => StatusCodes.Status404NotFound,
                "forbidden" => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }
        await WriteAudit("machine.updated", "Machine", machineId, null, result.Value,
            ["FR-LAB-002", "BR-OPS-006", "UC-014"], null, cancellationToken);
        return Ok(result.Value);
    }

    [HttpDelete("{labId:guid}/machines/{machineId:guid}")]
    [Authorize(Roles = Roles.LabManager)]
    public async Task<IActionResult> DeleteMachine(Guid labId, Guid machineId, CancellationToken cancellationToken)
    {
        var actorUserId = User.GetCustomerId();
        if (actorUserId is null) return Forbid();
        var result = await _sender.Send(new DeleteMachineCommand(labId, machineId, actorUserId.Value), cancellationToken);
        if (result.IsFailure)
        {
            var status = result.Error.Code switch
            {
                "not_found" => StatusCodes.Status404NotFound,
                "forbidden" => StatusCodes.Status403Forbidden,
                "conflict" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }
        await WriteAudit("machine.deleted", "Machine", machineId, null, null,
            ["FR-LAB-002", "BR-OPS-006", "UC-014"], null, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{labId:guid}/standing")]
    [Authorize(Roles = Roles.OpsManager)]
    public async Task<IActionResult> SetStanding(Guid labId, [FromBody] SetStandingRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetLabStandingCommand(labId, request.IsInGoodStanding), cancellationToken);
        if (result.IsFailure)
        {
            var status = result.Error.Code == "not_found" ? StatusCodes.Status404NotFound : StatusCodes.Status409Conflict;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }
        return NoContent();
    }

    [HttpPatch("{labId:guid}/capacity-limit")]
    [Authorize(Roles = Roles.OpsManager)]
    public async Task<IActionResult> SetCapacityLimit(Guid labId, [FromBody] SetCapacityLimitRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetLabCapacityLimitCommand(labId, request.MaxConcurrentJobs), cancellationToken);
        if (result.IsFailure)
        {
            var status = result.Error.Code == "not_found" ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }
        return NoContent();
    }

    private Task WriteAudit(
        string action,
        string entityType,
        Guid entityId,
        object? before,
        object? after,
        IReadOnlyList<string> requirementCodes,
        string? reason,
        CancellationToken cancellationToken) =>
        _audit.WriteAsync(new AuditLogWriteRequest(
            User.GetCustomerId(),
            User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "unknown",
            User.FindFirstValue(ClaimTypes.Role) ?? "unknown",
            AuditActionClasses.Configuration,
            action,
            entityType,
            entityId.ToString(),
            before,
            after,
            reason,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.TraceIdentifier,
            requirementCodes), cancellationToken);
}

public record RegisterLabRequest(
    string Name,
    string City,
    int TransitDaysToHub,
    string WorkingDays,
    string OpeningTime,
    string ClosingTime);

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
public record SetStandingRequest(bool IsInGoodStanding);
public record SetCapacityLimitRequest(int? MaxConcurrentJobs);
