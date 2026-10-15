using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using PrintGrid.Modules.Admin.Application.QcKnowledge;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/admin/qc")]
[Authorize(Policy = Policies.RequireAdmin)]
public sealed class AdminQcKnowledgeController(ISender sender, IAuditLogWriter audit) : ControllerBase
{
    [HttpGet("checklists")]
    public async Task<IActionResult> GetChecklists([FromQuery] string? grade, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetInspectionChecklistsQuery(grade), cancellationToken));

    [HttpPost("checklists/{grade}")]
    public async Task<IActionResult> CreateChecklist(string grade, SaveInspectionChecklistRequest request, CancellationToken cancellationToken)
    {
        var before = (await sender.Send(new GetInspectionChecklistsQuery(grade), cancellationToken)).FirstOrDefault(x => x.IsActive);
        var result = await sender.Send(new CreateInspectionChecklistVersionCommand(grade, request.Items, ActorId()), cancellationToken);
        if (!result.IsSuccess) return Conflict(ApiError(result.ErrorCode!, result.ErrorMessage!));
        await WriteAudit("inspection_checklist.version_created", "InspectionChecklistVersion", result.Value!.Id.ToString(),
            before, result.Value, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("defects")]
    public async Task<IActionResult> GetDefects([FromQuery] bool includeDeprecated = true, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetDefectTaxonomyQuery(includeDeprecated), cancellationToken));

    [HttpPut("defects/{code}")]
    public async Task<IActionResult> SaveDefect(string code, SaveDefectTaxonomyRequest request, CancellationToken cancellationToken)
    {
        var before = (await sender.Send(new GetDefectTaxonomyQuery(true), cancellationToken))
            .FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        var result = await sender.Send(new SaveDefectTaxonomyItemCommand(
            code, request.Name, request.Description, request.DefaultFaultAttribution,
            request.IsDeprecated, ActorId()), cancellationToken);
        if (!result.IsSuccess) return Conflict(ApiError(result.ErrorCode!, result.ErrorMessage!));
        await WriteAudit(before is null ? "defect_taxonomy.created" : "defect_taxonomy.updated",
            "DefectTaxonomyItem", result.Value!.Id.ToString(), before, result.Value, cancellationToken);
        return Ok(result.Value);
    }

    [HttpDelete("defects/{code}")]
    public async Task<IActionResult> DeleteDefect(string code, CancellationToken cancellationToken)
    {
        var before = (await sender.Send(new GetDefectTaxonomyQuery(true), cancellationToken))
            .FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        var result = await sender.Send(new DeleteDefectTaxonomyItemCommand(code), cancellationToken);
        if (!result.IsSuccess) return Conflict(ApiError(result.ErrorCode!, result.ErrorMessage!));
        await WriteAudit("defect_taxonomy.deleted", "DefectTaxonomyItem", before?.Id.ToString(), before, null, cancellationToken);
        return NoContent();
    }

    private Guid ActorId() => Guid.Parse(User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Task WriteAudit(string action, string entityType, string? entityId, object? oldValues, object? newValues, CancellationToken cancellationToken) =>
        audit.WriteAsync(new AuditLogWriteRequest(
            ActorId(), User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "unknown",
            User.FindFirstValue(ClaimTypes.Role) ?? "Admin", AuditActionClasses.Configuration,
            action, entityType, entityId, oldValues, newValues,
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(), TraceId: HttpContext.TraceIdentifier,
            RequirementCodes: RequirementCodes), cancellationToken);

    private static object ApiError(string code, string message) => new { error = new { code, message } };
    private static readonly string[] RequirementCodes = ["FR-ADMIN-005", "UC-024", "BR-QC-002", "BR-QC-004", "BR-CONFIG-001", "BR-CONFIG-002", "BR-CONFIG-003"];
}

public sealed record SaveInspectionChecklistRequest(IReadOnlyList<InspectionChecklistItemDto> Items);
public sealed record SaveDefectTaxonomyRequest(
    string Name, string? Description, string? DefaultFaultAttribution, bool IsDeprecated);
