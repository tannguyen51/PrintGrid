using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using PrintGrid.Modules.Admin.Application.Configuration;
using PrintGrid.Modules.Admin.Domain.Entities;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = Policies.RequireAdmin)]
public sealed class AdminConfigurationController(ISender sender, IAuditLogWriter audit) : ControllerBase
{
    [HttpGet("commercial-configurations")]
    public async Task<IActionResult> GetConfigurations(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCommercialConfigurationsQuery(), cancellationToken));

    [HttpPost("commercial-configurations")]
    public async Task<IActionResult> CreateConfiguration(CreateCommercialConfigurationRequest request, CancellationToken cancellationToken)
    {
        var created = await sender.Send(new CreateCommercialConfigurationCommand(
            request.Name, request.Parameters, request.ChangeSummary, ActorId()), cancellationToken);
        await WriteAudit("commercial_configuration.created", "CommercialConfigurationVersion", created.Id.ToString(),
            null, created, CommercialCodes, cancellationToken);
        return CreatedAtAction(nameof(GetConfigurations), new { id = created.Id }, created);
    }

    [HttpPost("commercial-configurations/{id:guid}/activate")]
    public async Task<IActionResult> ActivateConfiguration(Guid id, CancellationToken cancellationToken)
    {
        var before = (await sender.Send(new GetCommercialConfigurationsQuery(), cancellationToken)).FirstOrDefault(x => x.Id == id);
        var result = await sender.Send(new ActivateCommercialConfigurationCommand(id), cancellationToken);
        if (!result.IsSuccess) return Conflict(ApiError(result.ErrorCode!, result.ErrorMessage!));
        await WriteAudit("commercial_configuration.activated", "CommercialConfigurationVersion", id.ToString(),
            before, result.Value, CommercialCodes, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("catalog-items")]
    public async Task<IActionResult> GetCatalog([FromQuery] string? type, [FromQuery] bool includeDeprecated = true, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(type) && !SharedCatalogTypes.All.Contains(type))
            return BadRequest(ApiError("invalid_catalog_type", "Loại danh mục không hợp lệ."));
        return Ok(await sender.Send(new GetSharedCatalogQuery(type, includeDeprecated), cancellationToken));
    }

    [HttpPut("catalog-items/{type}/{code}")]
    public async Task<IActionResult> SaveCatalogItem(string type, string code, SaveSharedCatalogItemRequest request, CancellationToken cancellationToken)
    {
        if (!SharedCatalogTypes.All.Contains(type))
            return BadRequest(ApiError("invalid_catalog_type", "Loại danh mục không hợp lệ."));
        var before = (await sender.Send(new GetSharedCatalogQuery(type, true), cancellationToken))
            .FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        var result = await sender.Send(new SaveSharedCatalogItemCommand(
            type, code, request.Name, request.Description, request.UnitRate, request.IsPremium,
            request.Properties, request.IsDeprecated, ActorId()), cancellationToken);
        if (!result.IsSuccess) return Conflict(ApiError(result.ErrorCode!, result.ErrorMessage!));
        await WriteAudit(before is null ? "catalog_item.created" : "catalog_item.revised", "SharedCatalogItem",
            result.Value!.Id.ToString(), before, result.Value, CatalogCodes, cancellationToken);
        return Ok(result.Value);
    }

    [HttpDelete("catalog-items/{type}/{code}")]
    public async Task<IActionResult> DeleteCatalogItem(string type, string code, CancellationToken cancellationToken)
    {
        if (!SharedCatalogTypes.All.Contains(type))
            return BadRequest(ApiError("invalid_catalog_type", "Loại danh mục không hợp lệ."));
        var before = (await sender.Send(new GetSharedCatalogQuery(type, true), cancellationToken))
            .FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        var result = await sender.Send(new DeleteSharedCatalogItemCommand(type, code), cancellationToken);
        if (!result.IsSuccess) return Conflict(ApiError(result.ErrorCode!, result.ErrorMessage!));
        await WriteAudit("catalog_item.deleted", "SharedCatalogItem", before?.Id.ToString(), before, null, CatalogCodes, cancellationToken);
        return NoContent();
    }

    private Guid ActorId() => Guid.Parse(User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Task WriteAudit(string action, string entityType, string? entityId, object? oldValues, object? newValues,
        IReadOnlyList<string> codes, CancellationToken cancellationToken) =>
        audit.WriteAsync(new AuditLogWriteRequest(
            ActorId(), User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "unknown",
            User.FindFirstValue(ClaimTypes.Role) ?? "Admin", AuditActionClasses.Configuration,
            action, entityType, entityId, oldValues, newValues,
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(), TraceId: HttpContext.TraceIdentifier,
            RequirementCodes: codes), cancellationToken);

    private static object ApiError(string code, string message) => new { error = new { code, message } };

    private static readonly string[] CommercialCodes = ["FR-ADMIN-002", "UC-009", "BR-CONFIG-001", "BR-CONFIG-002", "BR-CONFIG-003"];
    private static readonly string[] CatalogCodes = ["FR-ADMIN-003", "UC-023", "BR-CONFIG-001", "BR-CONFIG-002", "BR-CONFIG-003"];
}

public sealed record CreateCommercialConfigurationRequest(
    string Name, CommercialParametersDto Parameters, string ChangeSummary);
public sealed record SaveSharedCatalogItemRequest(
    string Name, string? Description, decimal? UnitRate, bool IsPremium,
    JsonElement Properties, bool IsDeprecated);
