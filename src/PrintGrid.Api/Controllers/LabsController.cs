using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;
using PrintGrid.Modules.Scheduling.Application.Queries;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/labs")]
[Authorize(Policy = Policies.RequireProduction)]
public class LabsController : ControllerBase
{
    private readonly ISender _sender;

    public LabsController(ISender sender) => _sender = sender;

    private bool IsAdminOrOps => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.OpsManager);

    [HttpGet]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> GetLabs([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetLabsQuery(includeInactive), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyLab([FromQuery] bool allowUnclaimedFallback = true, CancellationToken cancellationToken = default)
    {
        var userId = User.GetCustomerId();
        if (userId is null)
            return Unauthorized();

        var result = await _sender.Send(new GetMyLabQuery(userId.Value, allowUnclaimedFallback), cancellationToken);
        return result.IsFailure ? ToErrorResult(result.Error) : Ok(result.Value);
    }

    [HttpGet("{labId:guid}")]
    public async Task<IActionResult> GetLab(Guid labId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLabQuery(labId), cancellationToken);
        return result.IsFailure ? ToErrorResult(result.Error) : Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> RegisterLab([FromBody] RegisterLabRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterLabCommand(request.Name, request.City, request.TransitDaysToHub),
            cancellationToken);

        if (result.IsFailure)
            return ToErrorResult(result.Error);

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

        return CreatedAtAction(nameof(GetLab), new { labId = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Admin/Ops approves a pending lab (PENDING -> ACTIVE). Blocked if the lab has 0 machines (AC02 / BR-OPS-006).
    /// </summary>
    [HttpPost("{labId:guid}/approve")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> ApproveLab(Guid labId, CancellationToken cancellationToken)
    {
        var userId = User.GetCustomerId();
        var result = await _sender.Send(new ApproveLabCommand(labId, userId), cancellationToken);
        return result.IsFailure ? ToErrorResult(result.Error) : Ok(result.Value);
    }

    [HttpPost("{labId:guid}/reject")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> RejectLab(Guid labId, [FromBody] RejectLabRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RejectLabCommand(labId, request.Reason), cancellationToken);
        return result.IsFailure ? ToErrorResult(result.Error) : Ok(result.Value);
    }

    [HttpPatch("{labId:guid}/active")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> SetActive(Guid labId, [FromBody] SetActiveRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetLabActiveCommand(labId, request.IsActive), cancellationToken);
        if (result.IsFailure)
            return ToErrorResult(result.Error);

        return NoContent();
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
                request.SupportedMaterials,
                User.GetCustomerId(),
                IsAdminOrOps),
            cancellationToken);

        if (result.IsFailure)
            return ToErrorResult(result.Error);

        return CreatedAtAction(nameof(GetLab), new { labId }, result.Value);
    }

    [HttpPut("{labId:guid}/machines/{machineId:guid}")]
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

        return result.IsFailure ? ToErrorResult(result.Error) : Ok(result.Value);
    }

    [HttpDelete("{labId:guid}/machines/{machineId:guid}")]
    public async Task<IActionResult> DeleteMachine(Guid labId, Guid machineId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteMachineCommand(labId, machineId, User.GetCustomerId(), IsAdminOrOps),
            cancellationToken);

        return result.IsFailure ? ToErrorResult(result.Error) : NoContent();
    }

    [HttpPatch("{labId:guid}/machines/{machineId:guid}/status")]
    public async Task<IActionResult> SetMachineStatus(Guid labId, Guid machineId, [FromBody] SetMachineStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SetMachineStatusCommand(labId, machineId, request.Status, User.GetCustomerId(), IsAdminOrOps),
            cancellationToken);

        return result.IsFailure ? ToErrorResult(result.Error) : NoContent();
    }

    [HttpPut("{labId:guid}/machines/{machineId:guid}/maintenance")]
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
    public async Task<IActionResult> GetTimeline(Guid labId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLabTimelineQuery(labId), cancellationToken);
        return result.IsFailure ? ToErrorResult(result.Error) : Ok(result.Value);
    }

    [HttpGet("{labId:guid}/performance")]
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
}

public record RegisterLabRequest(string Name, string City, int TransitDaysToHub);

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

public record SetMachineStatusRequest(MachineStatus Status);

public record ScheduleMaintenanceRequest(
    DateTime? MaintenanceFromUtc,
    DateTime? MaintenanceUntilUtc,
    string? Reason,
    bool Clear = false,
    bool SetMaintenanceStatusNow = false);