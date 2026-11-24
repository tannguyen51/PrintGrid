using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Api.Authorization;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/cura-profiles")]
[Authorize(Policy = Policies.RequireProduction)]
public sealed class CuraProfilesController(
    PrintGridDbContext db,
    IMachineProfileMatcher matcher,
    ISchedulingRegistryQueryService registryQueries,
    IConfiguration configuration,
    IAuditLogWriter audit) : ControllerBase
{
    private string EngineVersion => configuration["QuoteSlicer:EngineVersion"] ?? "5.11.0";

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await db.Set<CuraProfile>().AsNoTracking()
        .Where(p => p.IsActive && p.ApprovalStatus == CuraProfile.StatusApproved)
        .OrderBy(p => p.Manufacturer).ThenBy(p => p.MachineModel)
        .Select(p => new
        {
            p.Id, p.Name, p.Manufacturer, p.MachineModel, p.MaterialCode, p.Version, p.EngineVersion,
            p.NozzleDiameterMm, p.FilamentDiameterMm, p.ExtruderCount, p.MaximumColorSlots,
            p.WidthMm, p.DepthMm, p.HeightMm, p.HeatedBed, p.RequiredNozzleTemperatureC, p.RequiredBedTemperatureC
        })
        .ToListAsync(ct));

    [HttpGet("paged")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> ListPaged(
        [FromQuery] string? scope = "all",
        [FromQuery] string? search = null,
        [FromQuery] Guid? labId = null,
        [FromQuery] string? approvalStatus = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? manufacturerOrModel = null,
        [FromQuery] string? material = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default) =>
        Ok(await registryQueries.GetCuraProfilesPagedAsync(new CuraProfilePagedQueryFilter(
            scope, search, labId, approvalStatus, isActive, manufacturerOrModel, material, page, pageSize), ct));

    [HttpGet("my-proposals")]
    [Authorize(Roles = $"{Roles.LabManager},{Roles.LabOperator}")]
    public async Task<IActionResult> MyProposals(
        [FromQuery] string? search = null,
        [FromQuery] string? approvalStatus = null,
        [FromQuery] string? material = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var actor = User.GetCustomerId();
        if (actor is null) return Unauthorized();
        return Ok(await registryQueries.GetMyCuraProfileProposalsAsync(actor.Value,
            new MyCuraProfileProposalsFilter(search, approvalStatus, material, page, pageSize), ct));
    }

    [HttpGet("proposals/{id:guid}")]
    [Authorize(Roles = $"{Roles.LabManager},{Roles.LabOperator},{Roles.Admin}")]
    public async Task<IActionResult> GetProposal(Guid id, CancellationToken ct)
    {
        var detail = await registryQueries.GetCuraProfileDetailAsync(id, ct);
        if (detail is null) return NotFound();

        if (!User.IsInRole(Roles.Admin))
        {
            var actor = User.GetCustomerId();
            if (actor is null) return Unauthorized();
            if (!detail.SubmittedByLabId.HasValue) return Forbid();
            var isMember = await db.Set<Lab>().AsNoTracking()
                .AnyAsync(l => l.Id == detail.SubmittedByLabId.Value &&
                               (l.OwnerUserId == actor.Value || l.Staff.Any(s => s.UserId == actor.Value)), ct);
            if (!isMember) return Forbid();
        }

        return Ok(detail);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var detail = await registryQueries.GetCuraProfileDetailAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireAdmin)]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Import(ImportCuraProfileRequest request, CancellationToken ct)
    {
        try
        {
            if (request.EngineVersion != EngineVersion) return Invalid($"Cần profile tương thích CuraEngine {EngineVersion}.");
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
            await Log("cura_profile.imported", "CuraProfile", profile.Id, new { profile.Name, profile.Version }, null, ct);
            return Ok(new { profile.Id, profile.Name });
        }
        catch (Exception e) when (e is ArgumentException or System.Text.Json.JsonException or InvalidOperationException)
        { return Invalid(e.Message); }
    }

    [HttpPost("proposals")]
    [Authorize(Roles = $"{Roles.LabManager},{Roles.LabOperator}")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Propose(SubmitCuraProfileProposalRequest request, CancellationToken ct)
    {
        var actor = User.GetCustomerId();
        if (actor is null) return Unauthorized();

        var lab = await db.Set<Lab>().AsNoTracking()
            .SingleOrDefaultAsync(l => l.Id == request.LabId &&
                (l.OwnerUserId == actor.Value || l.Staff.Any(s => s.UserId == actor.Value)), ct);
        if (lab is null) return Forbid();

        if (request.TargetMachineId.HasValue)
        {
            var ownsMachine = await db.Set<Machine>().AsNoTracking()
                .AnyAsync(m => m.Id == request.TargetMachineId.Value && m.LabId == lab.Id, ct);
            if (!ownsMachine) return Invalid("Máy được liên kết không thuộc xưởng này.");
        }

        try
        {
            var profile = CuraProfileImporter.Parse(
                request.Name,
                request.Manufacturer,
                request.MachineModel,
                request.MaterialCode,
                request.Version,
                EngineVersion,
                request.ExtruderCount,
                request.SettingsJson,
                isActive: false,
                approvalStatus: CuraProfile.StatusPendingReview,
                submittedByLabId: lab.Id,
                submittedByUserId: actor.Value,
                targetMachineId: request.TargetMachineId,
                submissionNotes: request.SubmissionNotes,
                sourceFileName: request.SourceFileName);

            var duplicatePending = await db.Set<CuraProfile>().AsNoTracking()
                .AnyAsync(p => p.SubmittedByLabId == lab.Id
                               && p.ApprovalStatus == CuraProfile.StatusPendingReview
                               && p.Manufacturer.ToLower() == profile.Manufacturer.ToLower()
                               && p.MachineModel.ToLower() == profile.MachineModel.ToLower()
                               && p.MaterialCode == profile.MaterialCode
                               && p.Version.ToLower() == profile.Version.ToLower()
                               && p.NozzleDiameterMm == profile.NozzleDiameterMm, ct);
            if (duplicatePending)
                return Invalid("Xưởng đã có yêu cầu profile cùng hãng, model, vật liệu và phiên bản đang chờ Admin duyệt.");

            db.Set<CuraProfile>().Add(profile);
            await db.SaveChangesAsync(ct);
            await Log("cura_profile.proposed", "CuraProfile", profile.Id,
                new { profile.Name, profile.Version, profile.MaterialCode, lab.Id, request.TargetMachineId }, null, ct);
            return Ok(await registryQueries.GetCuraProfileDetailAsync(profile.Id, ct));
        }
        catch (Exception e) when (e is ArgumentException or System.Text.Json.JsonException or InvalidOperationException)
        { return Invalid(e.Message); }
    }

    [HttpPut("proposals/{id:guid}")]
    [Authorize(Roles = $"{Roles.LabManager},{Roles.LabOperator}")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> ResubmitProposal(Guid id, ResubmitCuraProfileProposalRequest request, CancellationToken ct)
    {
        var actor = User.GetCustomerId();
        if (actor is null) return Unauthorized();

        var profile = await db.Set<CuraProfile>().SingleOrDefaultAsync(p => p.Id == id, ct);
        if (profile is null) return NotFound();
        if (!profile.SubmittedByLabId.HasValue) return Forbid();

        var isMember = await db.Set<Lab>().AsNoTracking()
            .AnyAsync(l => l.Id == profile.SubmittedByLabId.Value &&
                           (l.OwnerUserId == actor.Value || l.Staff.Any(s => s.UserId == actor.Value)), ct);
        if (!isMember) return Forbid();

        if (request.TargetMachineId.HasValue)
        {
            var ownsMachine = await db.Set<Machine>().AsNoTracking()
                .AnyAsync(m => m.Id == request.TargetMachineId.Value && m.LabId == profile.SubmittedByLabId.Value, ct);
            if (!ownsMachine) return Invalid("Máy được liên kết không thuộc xưởng này.");
        }

        try
        {
            var parsedReplacement = CuraProfileImporter.Parse(
                request.Name,
                request.Manufacturer,
                request.MachineModel,
                request.MaterialCode,
                request.Version,
                EngineVersion,
                request.ExtruderCount,
                request.SettingsJson,
                isActive: false,
                approvalStatus: CuraProfile.StatusPendingReview,
                submittedByLabId: profile.SubmittedByLabId,
                submittedByUserId: actor.Value,
                targetMachineId: request.TargetMachineId,
                submissionNotes: request.SubmissionNotes,
                sourceFileName: request.SourceFileName);

            profile.ResubmitFrom(
                parsedReplacement,
                actor.Value,
                request.TargetMachineId,
                request.SubmissionNotes,
                request.SourceFileName);

            await db.SaveChangesAsync(ct);
            await Log("cura_profile.proposal_resubmitted", "CuraProfile", profile.Id,
                new { profile.Name, profile.Version, profile.MaterialCode, profile.SubmittedByLabId, profile.TargetMachineId }, null, ct);
            return Ok(await registryQueries.GetCuraProfileDetailAsync(profile.Id, ct));
        }
        catch (Exception e) when (e is ArgumentException or System.Text.Json.JsonException or InvalidOperationException)
        { return Invalid(e.Message); }
    }

    [HttpPost("proposals/{id:guid}/review")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> ReviewProposal(Guid id, ReviewCuraProfileProposalRequest request, CancellationToken ct)
    {
        var actor = User.GetCustomerId();
        if (actor is null) return Forbid();

        var profile = await db.Set<CuraProfile>().SingleOrDefaultAsync(p => p.Id == id, ct);
        if (profile is null) return NotFound();

        try
        {
            if (request.Approve)
            {
                profile.ApproveProposal(actor.Value, request.Reason);
                // Approving a profile only makes it active in the library and moves matching
                // unapproved machines to PendingReview; machines still need separate approval.
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                await db.SaveChangesAsync(ct);
                var machines = await db.Set<Machine>()
                    .Where(m => m.ProfileStatus != "Approved" && m.ProfileStatus != "Rejected")
                    .ToListAsync(ct);
                foreach (var machine in machines) await matcher.MatchAsync(machine, ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                await Log("cura_profile.proposal_approved", "CuraProfile", profile.Id,
                    new { profile.Name, profile.Version, profile.MaterialCode, profile.SubmittedByLabId }, request.Reason, ct);
            }
            else
            {
                profile.RejectProposal(actor.Value, request.Reason ?? "");
                await db.SaveChangesAsync(ct);
                await Log("cura_profile.proposal_rejected", "CuraProfile", profile.Id,
                    new { profile.Name, profile.Version, profile.MaterialCode, profile.SubmittedByLabId }, profile.ReviewReason, ct);
            }

            return Ok(await registryQueries.GetCuraProfileDetailAsync(profile.Id, ct));
        }
        catch (ArgumentException e)
        { return Invalid(e.Message); }
    }

    [HttpPatch("{id:guid}/active")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> SetActive(Guid id, SetCuraProfileActiveRequest request, CancellationToken ct)
    {
        var profile = await db.Set<CuraProfile>().SingleOrDefaultAsync(p => p.Id == id, ct);
        if (profile is null) return NotFound();

        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (request.IsActive) profile.Activate();
            else profile.Deactivate();
            await db.SaveChangesAsync(ct);

            var machinesToRematch = request.IsActive
                ? await db.Set<Machine>().Where(m => m.ProfileStatus != "Approved" && m.ProfileStatus != "Rejected").ToListAsync(ct)
                : await db.Set<Machine>().Where(m => m.CuraProfileIds.Contains(profile.Id) || (m.ProfileStatus != "Approved" && m.ProfileStatus != "Rejected")).ToListAsync(ct);
            foreach (var machine in machinesToRematch) await matcher.MatchAsync(machine, ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            await Log(request.IsActive ? "cura_profile.activated" : "cura_profile.deactivated",
                "CuraProfile", profile.Id, new { profile.Name, profile.Version, profile.IsActive }, null, ct);
            return Ok(await registryQueries.GetCuraProfileDetailAsync(profile.Id, ct));
        }
        catch (ArgumentException e)
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
        await Log("machine.profile_matched", "Machine", machineId, new { machine.ProfileStatus, machine.CuraProfileIds }, null, ct);
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
                var profiles = await db.Set<CuraProfile>()
                    .Where(p => machine.CuraProfileIds.Contains(p.Id)
                                && p.IsActive
                                && p.ApprovalStatus == CuraProfile.StatusApproved
                                && p.EngineVersion == EngineVersion)
                    .ToListAsync(ct);
                if (machine.SupportedMaterials.Count == 0 ||
                    !machine.SupportedMaterials.All(m => profiles.Any(p => p.MaterialCode == m && p.Matches(machine))))
                {
                    return Invalid("Máy chưa có đủ profile đã duyệt và đang kích hoạt cho tất cả vật liệu.");
                }
            }
            if (request.Reason?.Length > 1000) return Invalid("Lý do tối đa 1000 ký tự.");
            machine.ReviewProfile(request.Approve, actor.Value, request.Reason);
            await db.SaveChangesAsync(ct);
            await Log("machine.profile_reviewed", "Machine", machineId,
                new { machine.ProfileStatus, machine.ProfileReviewReason }, request.Reason, ct);
            return Ok(LabMappings.ToDto(machine));
        }
        catch (ArgumentException e) { return Invalid(e.Message); }
    }

    private BadRequestObjectResult Invalid(string message) => BadRequest(new { error = new { code = "validation", message } });

    private string CallerRole() =>
        User.IsInRole(Roles.Admin) ? Roles.Admin
        : User.IsInRole(Roles.LabManager) ? Roles.LabManager
        : User.IsInRole(Roles.LabOperator) ? Roles.LabOperator
        : User.IsInRole(Roles.OpsManager) ? Roles.OpsManager
        : "Staff";

    private Task Log(string action, string entity, Guid id, object data, string? reason, CancellationToken ct) =>
        audit.WriteAsync(new AuditLogWriteRequest(
            User.GetCustomerId(),
            User.FindFirstValue(ClaimTypes.Email) ?? "unknown",
            CallerRole(),
            AuditActionClasses.Configuration,
            action,
            entity,
            id.ToString(),
            null,
            data,
            reason,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.TraceIdentifier), ct);
}

public sealed record ImportCuraProfileRequest(
    string Name,
    string Manufacturer,
    string MachineModel,
    string MaterialCode,
    string Version,
    string EngineVersion,
    int ExtruderCount,
    string SettingsJson);

public sealed record SubmitCuraProfileProposalRequest(
    Guid LabId,
    string Name,
    string Manufacturer,
    string MachineModel,
    string MaterialCode,
    string Version,
    int ExtruderCount,
    string SettingsJson,
    Guid? TargetMachineId = null,
    string? SubmissionNotes = null,
    string? SourceFileName = null);

public sealed record ResubmitCuraProfileProposalRequest(
    string Name,
    string Manufacturer,
    string MachineModel,
    string MaterialCode,
    string Version,
    int ExtruderCount,
    string SettingsJson,
    Guid? TargetMachineId = null,
    string? SubmissionNotes = null,
    string? SourceFileName = null);

public sealed record ReviewCuraProfileProposalRequest(bool Approve, string? Reason = null);

public sealed record SetCuraProfileActiveRequest(bool IsActive);

public sealed record ReviewMachineProfileRequest(bool Approve, string? Reason);
