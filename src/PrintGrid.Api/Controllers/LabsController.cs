using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;
using PrintGrid.Modules.Scheduling.Application.Queries;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// Lab registry, onboarding/approval, machine CRUD and network operations
/// (FR-LAB-001/002/006/007, BR-OPS-005/006). This controller is the union of the
/// lab-hub screens (timeline, performance, maintenance) and the inspection/RBAC
/// batch (self-registration, approval, standing, capacity limit, audit trail).
/// </summary>
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

    private bool IsAdminOrOps => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.OpsManager);

    [HttpGet]
    [Authorize(Roles = $"{Roles.LabManager},{Roles.OpsManager},{Roles.Admin}")]
    public async Task<IActionResult> GetLabs([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        // A LabManager only ever sees its own lab(s); Ops/Admin see the whole network.
        var ownerUserId = User.IsInRole(Roles.LabManager) ? User.GetCustomerId() : null;
        if (User.IsInRole(Roles.LabManager) && ownerUserId is null) return Forbid();

        var result = await _sender.Send(new GetLabsQuery(includeInactive, ownerUserId), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpGet("me")]
    [Authorize(Policy = Policies.RequireProduction)]
    public async Task<IActionResult> GetMyLab([FromQuery] bool allowUnclaimedFallback = true, CancellationToken cancellationToken = default)
    {
        var userId = User.GetCustomerId();
        if (userId is null)
            return Unauthorized();

        var result = await _sender.Send(new GetMyLabQuery(userId.Value, allowUnclaimedFallback), cancellationToken);
        return result.IsFailure ? ToErrorResult(result.Error) : Ok(result.Value);
    }

    [HttpGet("{labId:guid}")]
    [Authorize(Roles = $"{Roles.LabManager},{Roles.OpsManager},{Roles.Admin}")]
    public async Task<IActionResult> GetLab(Guid labId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLabQuery(labId), cancellationToken);
        if (result.IsFailure)
            return ToErrorResult(result.Error);
        if (User.IsInRole(Roles.LabManager) && result.Value.OwnerUserId != User.GetCustomerId()) return Forbid();
        return Ok(result.Value);
    }

    /// <summary>
    /// Ops/Admin registers a lab directly into the network (no owner, ACTIVE immediately)
    /// or a LabManager onboards its own lab (owner assigned, PENDING until approved).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.LabManager},{Roles.OpsManager},{Roles.Admin}")]
    public async Task<IActionResult> RegisterLab([FromBody] RegisterLabRequest request, CancellationToken cancellationToken)
    {
        Guid? ownerUserId = null;
        if (User.IsInRole(Roles.LabManager))
        {
            ownerUserId = User.GetCustomerId();
            if (ownerUserId is null) return Forbid();
        }

        var result = await _sender.Send(
            new RegisterLabCommand(
                request.Name,
                request.City,
                request.TransitDaysToHub,
                ownerUserId,
                request.WorkingDays,
                request.OpeningTime,
                request.ClosingTime),
            cancellationToken);

        if (result.IsFailure)
            return ToErrorResult(result.Error);

        await WriteAudit("lab.registration_submitted", "Lab", result.Value.Id, null, result.Value,
            ["FR-LAB-001", "BR-OPS-005", "BR-OPS-006", "UC-014"], null, cancellationToken);

        return CreatedAtAction(nameof(GetLab), new { labId = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Lab self-registration (FR-LAB-001, BR-OPS-005/006): LabManager declares operating calendar,
    /// transit time to Hub, and profile details. Lab starts in PENDING status until Admin approves.
    /// </summary>
    [HttpPost("self-register")]
    public async Task<IActionResult> SelfRegister([FromBody] SelfRegisterLabRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetCustomerId();
        var result = await _sender.Send(
            new SelfRegisterLabCommand(
                request.Name,
                request.City,
                request.TransitDaysToHub,
                request.OperatingHours,
                userId,
                request.ContactPhone,
                request.Address),
            cancellationToken);

        if (result.IsFailure)
            return ToErrorResult(result.Error);

        await WriteAudit("lab.registration_submitted", "Lab", result.Value.Id, null, result.Value,
            ["FR-LAB-001", "BR-OPS-005", "BR-OPS-006", "UC-014"], null, cancellationToken);

        return CreatedAtAction(nameof(GetLab), new { labId = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Admin/Ops approves a pending lab (PENDING -> ACTIVE). Blocked if the lab has 0 machines (AC02 / BR-OPS-006).
    /// </summary>
    [HttpPost("{labId:guid}/approve")]
    [Authorize(Roles = $"{Roles.OpsManager},{Roles.Admin}")]
    public async Task<IActionResult> ApproveLab(Guid labId, CancellationToken cancellationToken)
    {
        var userId = User.GetCustomerId();
        var result = await _sender.Send(new ApproveLabCommand(labId, userId), cancellationToken);
        if (result.IsFailure)
            return ToErrorResult(result.Error);

        await WriteAudit("lab.approved", "Lab", labId, null, result.Value,
            ["FR-LAB-001", "BR-OPS-005", "BR-OPS-006", "UC-014"], null, cancellationToken);

        return Ok(result.Value);
    }

    [HttpPost("{labId:guid}/reject")]
    [Authorize(Roles = $"{Roles.OpsManager},{Roles.Admin}")]
    public async Task<IActionResult> RejectLab(Guid labId, [FromBody] RejectLabRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RejectLabCommand(labId, request.Reason), cancellationToken);
        if (result.IsFailure)
            return ToErrorResult(result.Error);

        await WriteAudit("lab.rejected", "Lab", labId, null, result.Value,
            ["FR-LAB-001", "BR-OPS-005", "UC-014"], request.Reason, cancellationToken);

        return Ok(result.Value);
    }

    [HttpPatch("{labId:guid}/active")]
    [Authorize(Roles = $"{Roles.OpsManager},{Roles.Admin}")]
    public async Task<IActionResult> SetActive(Guid labId, [FromBody] SetActiveRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetLabActiveCommand(labId, request.IsActive), cancellationToken);
        if (result.IsFailure)
            return ToErrorResult(result.Error);

        await WriteAudit(request.IsActive ? "lab.activated" : "lab.deactivated", "Lab", labId, null,
            new { request.IsActive }, ["FR-LAB-001", "BR-OPS-005", "BR-OPS-006", "UC-014"], null, cancellationToken);

        return NoContent();
    }

    [HttpPatch("{labId:guid}/standing")]
    [Authorize(Roles = Roles.OpsManager)]
    public async Task<IActionResult> SetStanding(Guid labId, [FromBody] SetStandingRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetLabStandingCommand(labId, request.IsInGoodStanding), cancellationToken);
        if (result.IsFailure)
            return ToErrorResult(result.Error);

        await WriteAudit(request.IsInGoodStanding ? "lab.standing_restored" : "lab.standing_suspended",
            "Lab", labId, null, new { request.IsInGoodStanding }, ["FR-LAB-007", "UC-014"], null, cancellationToken);

        return NoContent();
    }

    [HttpPatch("{labId:guid}/capacity-limit")]
    [Authorize(Roles = Roles.OpsManager)]
    public async Task<IActionResult> SetCapacityLimit(Guid labId, [FromBody] SetCapacityLimitRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetLabCapacityLimitCommand(labId, request.MaxConcurrentJobs), cancellationToken);
        if (result.IsFailure)
            return ToErrorResult(result.Error);

        await WriteAudit("lab.capacity_limit_set", "Lab", labId, null,
            new { request.MaxConcurrentJobs }, ["FR-LAB-007", "UC-014"], null, cancellationToken);

        return NoContent();
    }

    [HttpPost("{labId:guid}/machines")]
    [Authorize(Policy = Policies.RequireProduction)]
    public async Task<IActionResult> RegisterMachine(Guid labId, [FromBody] RegisterMachineRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = User.GetCustomerId();
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
                actorUserId,
                IsAdminOrOps,
                actorUserId),
            cancellationToken);

        if (result.IsFailure)
            return ToErrorResult(result.Error);

        await WriteAudit("machine.registered", "Machine", result.Value.Id, null, result.Value,
            ["FR-LAB-002", "BR-OPS-006", "UC-014"], null, cancellationToken);

        return CreatedAtAction(nameof(GetLab), new { labId }, result.Value);
    }

    [HttpPut("{labId:guid}/machines/{machineId:guid}")]
    [Authorize(Policy = Policies.RequireProduction)]
    public async Task<IActionResult> UpdateMachine(
        Guid labId,
        Guid machineId,
        [FromBody] RegisterMachineRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateMachineCommand(
                labId,
                machineId,
                request.Name,
                request.Model,
                request.Technology,
                request.BuildWidthMm,
                request.BuildDepthMm,
                request.BuildHeightMm,
                request.MinLayerHeightMm,
                request.AchievableToleranceMm,
                request.SupportedMaterials,
                User.GetCustomerId(),
                IsAdminOrOps),
            cancellationToken);

        if (result.IsFailure)
            return ToErrorResult(result.Error);

        await WriteAudit("machine.updated", "Machine", machineId, null, result.Value,
            ["FR-LAB-002", "BR-OPS-006", "UC-014"], null, cancellationToken);

        return Ok(result.Value);
    }

    [HttpDelete("{labId:guid}/machines/{machineId:guid}")]
    [Authorize(Policy = Policies.RequireProduction)]
    public async Task<IActionResult> DeleteMachine(Guid labId, Guid machineId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteMachineCommand(labId, machineId, User.GetCustomerId(), IsAdminOrOps),
            cancellationToken);

        if (result.IsFailure)
            return ToErrorResult(result.Error);

        await WriteAudit("machine.deleted", "Machine", machineId, null, null,
            ["FR-LAB-002", "BR-OPS-006", "UC-014"], null, cancellationToken);

        return NoContent();
    }

    [HttpPatch("{labId:guid}/machines/{machineId:guid}/status")]
    [Authorize(Policy = Policies.RequireProduction)]
    public async Task<IActionResult> SetMachineStatus(Guid labId, Guid machineId, [FromBody] SetMachineStatusRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = User.GetCustomerId();
        var result = await _sender.Send(
            new SetMachineStatusCommand(labId, machineId, request.Status, actorUserId, IsAdminOrOps, actorUserId),
            cancellationToken);

        return result.IsFailure ? ToErrorResult(result.Error) : NoContent();
    }

    [HttpPut("{labId:guid}/machines/{machineId:guid}/maintenance")]
    [Authorize(Policy = Policies.RequireProduction)]
    public async Task<IActionResult> ScheduleMaintenance(
        Guid labId,
        Guid machineId,
        [FromBody] ScheduleMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ScheduleMachineMaintenanceCommand(
                labId,
                machineId,
                request.MaintenanceFromUtc,
                request.MaintenanceUntilUtc,
                request.Reason,
                request.Clear,
                request.SetMaintenanceStatusNow,
                User.GetCustomerId(),
                IsAdminOrOps),
            cancellationToken);

        return result.IsFailure ? ToErrorResult(result.Error) : Ok(result.Value);
    }

    [HttpGet("{labId:guid}/timeline")]
    [Authorize(Policy = Policies.RequireProduction)]
    public async Task<IActionResult> GetTimeline(Guid labId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLabTimelineQuery(labId), cancellationToken);
        return result.IsFailure ? ToErrorResult(result.Error) : Ok(result.Value);
    }

    [HttpGet("{labId:guid}/performance")]
    [Authorize(Policy = Policies.RequireProduction)]
    public async Task<IActionResult> GetPerformance(Guid labId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLabPerformanceQuery(labId), cancellationToken);
        return result.IsFailure ? ToErrorResult(result.Error) : Ok(result.Value);
    }

    private IActionResult ToErrorResult(Error error)
    {
        var status = error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            "forbidden" => StatusCodes.Status403Forbidden,
            "unauthorized" => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };
        return StatusCode(status, new { error = new { code = error.Code, message = error.Message } });
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
    string WorkingDays = "Monday,Tuesday,Wednesday,Thursday,Friday",
    string OpeningTime = "08:00",
    string ClosingTime = "17:00");

public record SelfRegisterLabRequest(
    string Name,
    string City,
    int TransitDaysToHub,
    string OperatingHours,
    string? ContactPhone = null,
    string? Address = null);

public record RejectLabRequest(string Reason);

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

public record SetStandingRequest(bool IsInGoodStanding);

public record SetCapacityLimitRequest(int? MaxConcurrentJobs);

public record SetMachineStatusRequest(MachineStatus Status);

public record ScheduleMaintenanceRequest(
    DateTime? MaintenanceFromUtc,
    DateTime? MaintenanceUntilUtc,
    string? Reason,
    bool Clear = false,
    bool SetMaintenanceStatusNow = false);
