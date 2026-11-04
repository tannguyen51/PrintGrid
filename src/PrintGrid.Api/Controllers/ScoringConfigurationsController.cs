using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using PrintGrid.Modules.Scheduling.Application.Commands.ScoringConfiguration;
using System.Security.Claims;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/admin/scoring-configurations")]
[Authorize(Policy = Policies.RequireAdmin)]
public sealed class ScoringConfigurationsController(ISender sender, IAuditLogWriter audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetScoringConfigurationsQuery(), ct);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Activate(ActivateScoringConfigurationRequest request, CancellationToken ct)
    {
        var actorId = User.GetCustomerId();
        if (actorId is null) return Forbid();
        var result = await sender.Send(new ActivateScoringConfigurationCommand(
            request.Version, request.DueDateSlack, request.LabReliability,
            request.LoadBalance, request.Cost, request.Transit, request.Reason, actorId.Value), ct);
        if (result.IsSuccess)
        {
            // BR-CONFIG-002: every configuration change is audited (user, time, old/new).
            await audit.WriteAsync(new AuditLogWriteRequest(
                actorId.Value,
                User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "unknown",
                User.FindFirstValue(ClaimTypes.Role) ?? "Admin",
                AuditActionClasses.Configuration,
                "scoring_configuration.activated",
                "ScoringWeightConfiguration",
                request.Version,
                null,
                result.Value,
                Reason: request.Reason,
                IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                TraceId: HttpContext.TraceIdentifier,
                RequirementCodes: ["FR-ADMIN-002", "UC-009", "BR-CONFIG-002"]), ct);
            return Ok(result.Value);
        }
        var status = result.Error.Code == "conflict" ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
    }
}

public sealed record ActivateScoringConfigurationRequest(
    string Version, decimal DueDateSlack, decimal LabReliability,
    decimal LoadBalance, decimal Cost, decimal Transit, string Reason);
