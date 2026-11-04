using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using PrintGrid.Modules.Customer.Application.Models;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;
using System.Security.Claims;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// Content-moderation queue for customer models. An Operations Manager (or Admin) inspects the
/// stored file and approves or rejects it before the customer can quote/order it. This is the
/// only role that may read another customer's model file, and every read is audited.
/// </summary>
[ApiController]
[Route("api/v1/model-reviews")]
[Authorize(Policy = Policies.RequireQuoteReview)]
public sealed class ModelReviewsController(
    ISender sender,
    IModelRepository models,
    IFileStorage files,
    IAuditLogWriter audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQueue(
        [FromQuery] string? status = "Pending",
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetModelReviewsQuery(status, search), cancellationToken);
        return result.IsFailure ? ToErrorResult(result.Error) : Ok(result.Value);
    }

    [HttpGet("{modelId:guid}")]
    public async Task<IActionResult> Get(Guid modelId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetModelReviewQuery(modelId), cancellationToken);
        return result.IsFailure ? ToErrorResult(result.Error) : Ok(result.Value);
    }

    /// <summary>Reviewer access to the stored file — the reviewer cannot approve what they cannot see.</summary>
    [HttpGet("{modelId:guid}/file")]
    public async Task<IActionResult> GetFile(Guid modelId, CancellationToken cancellationToken)
    {
        var model = await models.GetByIdAsync(modelId, cancellationToken);
        if (model is null)
            return NotFound(new { error = new { code = "not_found", message = "Model not found" } });

        if (string.IsNullOrWhiteSpace(model.StorageKey))
            return NotFound(new { error = new { code = "file_not_stored", message = "Model này chưa có file lưu trữ." } });

        var separator = model.StorageKey.IndexOf('/');
        if (separator <= 0 || separator == model.StorageKey.Length - 1)
            return NotFound(new { error = new { code = "file_not_stored", message = "Đường dẫn file lưu trữ không hợp lệ." } });

        var stream = await files.DownloadAsync(
            model.StorageKey[..separator], model.StorageKey[(separator + 1)..], cancellationToken);

        await audit.WriteAsync(new AuditLogWriteRequest(
            User.GetCustomerId(),
            User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "unknown",
            User.FindFirstValue(ClaimTypes.Role) ?? "unknown",
            AuditActionClasses.ModelAccess,
            "model.file_reviewed",
            "Model",
            modelId.ToString(),
            Reason: "Content moderation review",
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            TraceId: HttpContext.TraceIdentifier,
            RequirementCodes: ["FR-CUST-002", "BR-ACCESS-002", "UC-005"]), cancellationToken);

        return File(stream, ContentTypeFor(Path.GetExtension(model.StorageKey)), enableRangeProcessing: true);
    }

    [HttpPost("{modelId:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid modelId,
        [FromBody] ModelApproveRequest? request,
        CancellationToken cancellationToken)
    {
        var reviewerId = User.GetCustomerId();
        if (reviewerId is null) return Forbid();

        var result = await sender.Send(
            new ApproveModelCommand(modelId, reviewerId.Value, request?.Note), cancellationToken);
        if (result.IsFailure) return ToErrorResult(result.Error);

        await WriteAudit("model.review_approved", modelId, request?.Note, cancellationToken);
        return NoContent();
    }

    [HttpPost("{modelId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid modelId,
        [FromBody] ModelRejectRequest request,
        CancellationToken cancellationToken)
    {
        var reviewerId = User.GetCustomerId();
        if (reviewerId is null) return Forbid();

        var result = await sender.Send(
            new RejectModelCommand(modelId, reviewerId.Value, request.Reason), cancellationToken);
        if (result.IsFailure) return ToErrorResult(result.Error);

        await WriteAudit("model.review_rejected", modelId, request.Reason, cancellationToken);
        return NoContent();
    }

    private Task WriteAudit(string action, Guid modelId, string? reason, CancellationToken cancellationToken) =>
        audit.WriteAsync(new AuditLogWriteRequest(
            User.GetCustomerId(),
            User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "unknown",
            User.FindFirstValue(ClaimTypes.Role) ?? "unknown",
            AuditActionClasses.ManualIntervention,
            action,
            "Model",
            modelId.ToString(),
            Reason: reason,
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            TraceId: HttpContext.TraceIdentifier,
            RequirementCodes: ["FR-CUST-002", "BR-ACCESS-002", "UC-005"]), cancellationToken);

    private static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".stl" => "model/stl",
        ".obj" => "model/obj",
        ".3mf" => "model/3mf",
        ".glb" => "model/gltf-binary",
        _ => "application/octet-stream"
    };

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

public sealed record ModelApproveRequest(string? Note = null);

public sealed record ModelRejectRequest(string Reason);
