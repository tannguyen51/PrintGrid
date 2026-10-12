using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Modules.Admin.Application.AuditLogs;

namespace PrintGrid.Api.Controllers;

/// <summary>Immutable audit trail viewer (FR-ADMIN-004 / UC-025).</summary>
[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Roles = "Admin")]
public sealed class AuditLogsController(ISender sender, IAuditLogWriter writer) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AuditLogPageDto>> Get(
        [FromQuery] string? actor,
        [FromQuery] string? actionClass,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc is not null && toUtc is not null && fromUtc > toUtc)
            return BadRequest(new { error = new { code = "invalid_date_range", message = "Ngày bắt đầu phải trước ngày kết thúc." } });

        return Ok(await sender.Send(
            new GetAuditLogsQuery(actor, actionClass, fromUtc, toUtc, page, Math.Clamp(pageSize, 1, 100)),
            cancellationToken));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? actor,
        [FromQuery] string? actionClass,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        if (fromUtc is not null && toUtc is not null && fromUtc > toUtc)
            return BadRequest(new { error = new { code = "invalid_date_range", message = "Ngày bắt đầu phải trước ngày kết thúc." } });

        var result = await sender.Send(new GetAuditLogsQuery(actor, actionClass, fromUtc, toUtc, 1, 10_000), cancellationToken);
        var csv = new StringBuilder("time_utc,actor,role,action_class,action,entity_type,entity_id,ip\r\n");
        foreach (var item in result.Items)
        {
            csv.AppendJoin(',',
                Csv(item.CreatedAtUtc.ToString("O")), Csv(item.ActorDisplay), Csv(item.ActorRole),
                Csv(item.ActionClass), Csv(item.Action), Csv(item.EntityType), Csv(item.EntityId),
                Csv(item.IpAddress));
            csv.Append("\r\n");
        }

        await writer.WriteAsync(new AuditLogWriteRequest(
            ActorId(), ActorDisplay(), ActorRole(), AuditActionClasses.Export, "audit_log.export",
            "AuditLog", null, null, new { result.Total, actor, actionClass, fromUtc, toUtc },
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(), TraceId: HttpContext.TraceIdentifier,
            RequirementCodes: ["FR-ADMIN-004", "UC-025", "BR-CONFIG-002", "BR-CONFIG-004"]), cancellationToken);

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv; charset=utf-8", $"printgrid-audit-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private Guid? ActorId() => Guid.TryParse(User.FindFirst("id")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    private string ActorDisplay() => User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "unknown";
    private string ActorRole() => User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Admin";
    private static string Csv(string? value)
    {
        var safe = value ?? string.Empty;
        // Prevent spreadsheet formula execution when an exported actor/entity begins
        // with a formula marker. The apostrophe is intentionally part of the CSV value.
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@') safe = $"'{safe}";
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }
}
