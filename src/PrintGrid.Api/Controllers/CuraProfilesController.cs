using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Api.Authorization;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using System.Security.Claims;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/cura-profiles")]
[Authorize(Policy = Policies.RequireProduction)]
public sealed class CuraProfilesController(PrintGridDbContext db, IMachineProfileMatcher matcher,
    IConfiguration configuration, IAuditLogWriter audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await db.Set<CuraProfile>().AsNoTracking()
        .Where(p => CuraProfile.IsActive).OrderBy(p => p.Manufacturer).ThenBy(p => p.MachineModel)
        .Select(p => new { p.Id, p.Name, p.Manufacturer, p.MachineModel, p.MaterialCode, p.Version, p.EngineVersion,
            p.NozzleDiameterMm, p.FilamentDiameterMm, p.ExtruderCount, p.MaximumColorSlots,
            p.WidthMm, p.DepthMm, p.HeightMm, p.HeatedBed, p.RequiredNozzleTemperatureC, p.RequiredBedTemperatureC })
        .ToListAsync(ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var profile = await db.Set<CuraProfile>().AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireAdmin)]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Import(ImportCuraProfileRequest request, CancellationToken ct)
    {
        try
        {
            var engineVersion = configuration["QuoteSlicer:EngineVersion"] ?? "5.11.0";
            if (request.EngineVersion != engineVersion) return Invalid($"Cần profile tương thích CuraEngine {engineVersion}.");
            var profile = CuraProfileImporter.Parse(request.Name, request.Manufacturer, request.MachineModel,
                request.MaterialCode, request.Version, request.EngineVersion, request.ExtruderCount, request.SettingsJson);
            db.Set<CuraProfile>().Add(profile);
            // Save and rematch atomically. Approved machines keep their frozen profile versions.
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.SaveChangesAsync(ct);
            var machines = await db.Set<Machine>().Where(m => m.ProfileStatus != "Approved" && m.ProfileStatus != "Rejected").ToListAsync(ct);
            foreach (var machine in machines) await matcher.MatchAsync(machine, ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            await Log("cura_profile.imported", "CuraProfile", profile.Id, new { profile.Name, profile.Version }, ct);
            return Ok(new { profile.Id, profile.Name });
        }
        catch (Exception e) when (e is ArgumentException or System.Text.Json.JsonException or InvalidOperationException)
        { return Invalid(e.Message); }
    }

    [HttpPost("machines/{machineId:guid}/match")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Match(Guid machineId, CancellationToken ct)
    {
        var machine = await db.Set<Machine>().SingleOrDefaultAsync(m => m.Id == machineId, ct);
        if (machine is null) return NotFound();
        await matcher.MatchAsync(machine, ct);
        await db.SaveChangesAsync(ct);
        await Log("machine.profile_matched", "Machine", machineId, new { machine.ProfileStatus, machine.CuraProfileIds }, ct);
        return Ok(LabMappings.ToDto(machine));
    }

    [HttpPost("machines/{machineId:guid}/review")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Review(Guid machineId, ReviewMachineProfileRequest request, CancellationToken ct)
    {
        var actor = User.GetCustomerId();
        if (actor is null) return Forbid();
        var machine = await db.Set<Machine>().SingleOrDefaultAsync(m => m.Id == machineId, ct);
        if (machine is null) return NotFound();
        try
        {
            if (request.Approve)
            {
                var profiles = await db.Set<CuraProfile>().Where(p => machine.CuraProfileIds.Contains(p.Id)).ToListAsync(ct);
                if (!machine.SupportedMaterials.All(m => profiles.Any(p => p.MaterialCode == m && p.Matches(machine))) || machine.SupportedMaterials.Count == 0)
                    return Invalid("Máy chưa có đủ profile phù hợp cho tất cả vật liệu.");
            }
            if (request.Reason?.Length > 1000) return Invalid("Lý do tối đa 1000 ký tự.");
            machine.ReviewProfile(request.Approve, actor.Value, request.Reason);
            await db.SaveChangesAsync(ct);
            await Log("machine.profile_reviewed", "Machine", machineId, new { machine.ProfileStatus, machine.ProfileReviewReason }, ct);
            return Ok(LabMappings.ToDto(machine));
        }
        catch (ArgumentException e) { return Invalid(e.Message); }
    }

    private BadRequestObjectResult Invalid(string message) => BadRequest(new { error = new { code = "validation", message } });
    private Task Log(string action, string entity, Guid id, object data, CancellationToken ct) => audit.WriteAsync(new AuditLogWriteRequest(
        User.GetCustomerId(), User.FindFirstValue(ClaimTypes.Email) ?? "unknown", "Admin", AuditActionClasses.Configuration,
        action, entity, id.ToString(), null, data, TraceId: HttpContext.TraceIdentifier), ct);
}

public sealed record ImportCuraProfileRequest(string Name, string Manufacturer, string MachineModel, string MaterialCode,
    string Version, string EngineVersion, int ExtruderCount, string SettingsJson);
public sealed record ReviewMachineProfileRequest(bool Approve, string? Reason);
