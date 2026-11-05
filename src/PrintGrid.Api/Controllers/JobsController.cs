using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Queries;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Modules.Customer.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[Authorize]
public class JobsController(
    ISender sender,
    IFileStorage files,
    IOptions<MinioOptions> minio,
    IOrderRepository orders,
    IModelRepository models,
    IAuditLogWriter audit)
    : ControllerBase
{
    private readonly MinioOptions _minio = minio.Value;

    /// <summary>True for the two lab roles, which are scoped to one lab (BR-ACCESS-003/004).</summary>
    private bool CallerIsLab =>
        User.IsInRole(Roles.LabManager) || User.IsInRole(Roles.LabOperator);

    /// <summary>
    /// Resolves the lab the caller belongs to. Legacy labs without an owner fall back to the
    /// first unclaimed lab, matching GetMyLabQuery's existing behaviour.
    /// </summary>
    private async Task<Guid?> CallerLabIdAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetCustomerId();
        if (userId is null) return null;
        var result = await sender.Send(new GetMyLabQuery(userId.Value), cancellationToken);
        return result.IsSuccess ? result.Value?.Id : null;
    }

    /// <summary>
    /// BR-ACCESS-001/003: a lab may only touch a job currently assigned to it. Returns a
    /// Forbid result when it may not, or null when the action may proceed. Ops/hub pass.
    /// A missing job is left to the command so the caller still gets its 404.
    /// </summary>
    private async Task<IActionResult?> ForbidIfNotCallersJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (!CallerIsLab) return null;

        var labId = await CallerLabIdAsync(cancellationToken);
        if (labId is null) return Forbid();

        var job = await sender.Send(new GetJobQuery(jobId), cancellationToken);
        if (job.IsFailure) return null;

        return job.Value.LabId == labId ? null : Forbid();
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireProduction)]
    // Lab/Hub/Ops each read the job board — they filter by status in the UI.
    public async Task<IActionResult> GetJobs([FromQuery] JobStatus? status, CancellationToken cancellationToken)
    {
        // A lab only sees work placed at its own lab; Ops/Hub keep the network-wide board.
        Guid? labFilter = null;
        if (CallerIsLab)
        {
            labFilter = await CallerLabIdAsync(cancellationToken);
            if (labFilter is null) return Ok(Array.Empty<JobDto>());
        }

        var result = await sender.Send(new GetJobsQuery(status ?? JobStatus.Pending, labFilter), cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        var jobs = result.Value;
        if (status == JobStatus.AwaitingInspection &&
            (User.IsInRole(Roles.HubQc) || User.IsInRole(Roles.HubFulfillment)))
        {
            jobs = [.. jobs.Where(j => j.QcProofStatus == "Approved")];
        }

        return Ok(jobs);
    }

    [HttpPost("{jobId:guid}/accept")]
    [Authorize(Policy = Policies.RequireLab)]
    public async Task<IActionResult> Accept(Guid jobId, CancellationToken cancellationToken)
    {
        if (await ForbidIfNotCallersJobAsync(jobId, cancellationToken) is { } forbidden) return forbidden;
        var result = await sender.Send(new AcceptJobCommand(jobId), cancellationToken);
        return ToResult(result);
    }

    /// <summary>
    /// Streams the model file behind a job so the lab can actually print it (FR-LAB-004):
    /// the model storage is Customer-scoped, so production roles resolve the file through
    /// their job instead. Internal roles only — RequireProduction excludes Customer.
    /// </summary>
    [HttpGet("{jobId:guid}/file")]
    [Authorize(Policy = Policies.RequireProduction)]
    public async Task<IActionResult> GetJobFile(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await sender.Send(new GetJobQuery(jobId), cancellationToken);
        if (job.IsFailure)
            return NotFound(new { error = new { code = "not_found", message = job.Error.Message } });

        // BR-ACCESS-003/004: only the lab currently assigned to the job may read the source
        // file, and only while the job is live (+4h grace after completion). Hub inspects the
        // physical part, never the source CAD; Ops/Admin review models through the dedicated
        // /api/v1/model-reviews endpoint, so they do not browse production job files.
        if (!CallerIsLab) return Forbid();

        {
            var labId = await CallerLabIdAsync(cancellationToken);
            if (labId is null || job.Value.LabId != labId || job.Value is { Status: nameof(JobStatus.Completed), CompletedAtUtc: { } completedAt }
                && DateTime.UtcNow - completedAt > TimeSpan.FromHours(4))
            {
                return Forbid();
            }
        }

        var order = await orders.GetByItemIdAsync(job.Value.OrderItemId, cancellationToken);
        var item = order?.Items.FirstOrDefault(i => i.Id == job.Value.OrderItemId);
        var model = item is null ? null : await models.GetByIdAsync(item.ModelId, cancellationToken);

        var storageKey = model?.StorageKey;
        if (string.IsNullOrWhiteSpace(storageKey))
            return NotFound(new { error = new { code = "file_not_stored", message = "Job này không trỏ tới file lưu trữ nào." } });

        // StorageKey is "{bucket}/{objectName}" — same contract as the model upload/download.
        var separator = storageKey.IndexOf('/');
        if (separator <= 0 || separator == storageKey.Length - 1)
            return NotFound(new { error = new { code = "file_not_stored", message = "Đường dẫn file lưu trữ không hợp lệ." } });

        var extension = Path.GetExtension(storageKey).ToLowerInvariant();
        var contentType = extension switch
        {
            ".stl" => "model/stl",
            ".obj" => "model/obj",
            ".3mf" => "model/3mf",
            ".glb" => "model/gltf-binary",
            _ => "application/octet-stream"
        };

        var stream = await files.DownloadAsync(
            storageKey[..separator], storageKey[(separator + 1)..], cancellationToken);

        // BR-ACCESS-002: every lab model-file download is logged (who, when, job, lab, IP).
        await audit.WriteAsync(new AuditLogWriteRequest(
            User.GetCustomerId(),
            User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "unknown",
            User.FindFirstValue(ClaimTypes.Role) ?? "unknown",
            AuditActionClasses.ModelAccess,
            "model.file_downloaded",
            "Job",
            jobId.ToString(),
            Reason: $"Lab file access for job {jobId}",
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            TraceId: HttpContext.TraceIdentifier,
            RequirementCodes: ["BR-ACCESS-001", "BR-ACCESS-002", "BR-ACCESS-004"]), cancellationToken);

        return File(stream, contentType, enableRangeProcessing: true);
    }

    [HttpPost("{jobId:guid}/decline")]
    [Authorize(Policy = Policies.RequireLab)]
    public async Task<IActionResult> Decline(Guid jobId, [FromBody] DeclineJobRequest request, CancellationToken cancellationToken)
    {
        if (await ForbidIfNotCallersJobAsync(jobId, cancellationToken) is { } forbidden) return forbidden;
        var result = await sender.Send(new DeclineJobCommand(jobId, request.Reason), cancellationToken);
        return ToResult(result);
    }

    [HttpPost("{jobId:guid}/start")]
    [Authorize(Policy = Policies.RequireLab)]
    public async Task<IActionResult> Start(Guid jobId, CancellationToken cancellationToken)
    {
        if (await ForbidIfNotCallersJobAsync(jobId, cancellationToken) is { } forbidden) return forbidden;
        var result = await sender.Send(new StartJobCommand(jobId), cancellationToken);
        return ToResult(result);
    }

    [HttpPost("{jobId:guid}/complete")]
    [Authorize(Policy = Policies.RequireLab)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(26 * 1024 * 1024)]
    public async Task<IActionResult> Complete(Guid jobId, [FromForm] CompleteJobRequest request, CancellationToken cancellationToken)
    {
        if (await ForbidIfNotCallersJobAsync(jobId, cancellationToken) is { } forbidden) return forbidden;
        if (request.Photos is null || request.Photos.Count is < 1 or > 5)
            return BadRequest(new { error = new { code = "validation_error", message = "Tải lên từ 1 đến 5 ảnh QC." } });
        if (request.Photos.Any(photo =>
                photo.Length == 0 ||
                photo.Length > 5 * 1024 * 1024 ||
                !photo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new { error = new { code = "validation_error", message = "Mỗi ảnh QC phải là tệp ảnh không quá 5 MB." } });
        }

        var photoKeys = new List<string>();
        foreach (var photo in request.Photos)
        {
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var objectName = $"qc/{jobId}/{Guid.NewGuid()}{extension}";
            await using var stream = photo.OpenReadStream();
            await files.UploadAsync(_minio.PhotoBucket, objectName, stream, photo.ContentType, cancellationToken);
            photoKeys.Add(objectName);
        }

        var result = await sender.Send(
            new CompleteJobCommand(jobId, request.ActualPrintMinutes, CompleteJobRequest.SelfReport, photoKeys, request.ActualMaterialGrams),
            cancellationToken);
        if (!result.IsFailure) return ToResult(result);
        foreach (var key in photoKeys)
            await files.DeleteAsync(_minio.PhotoBucket, key, cancellationToken);
        return ToResult(result);
    }

    /// <summary>
    /// Lab reports a mid-production fault (machine breakdown, defective part, out of material).
    /// The job is closed as Failed and an urgent reprint is created, charged to the lab.
    /// </summary>
    [HttpPost("{jobId:guid}/fail")]
    [Authorize(Policy = Policies.RequireLab)]
    public async Task<IActionResult> ReportFailure(
        Guid jobId,
        [FromBody] ReportJobFailureRequest request,
        CancellationToken cancellationToken)
    {
        if (await ForbidIfNotCallersJobAsync(jobId, cancellationToken) is { } forbidden) return forbidden;
        var result = await sender.Send(
            new ReportJobFailureCommand(jobId, request.Reason), cancellationToken);

        if (!result.IsFailure) return Ok(result.Value);
        var status = result.Error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });

    }

    [HttpPost("{jobId:guid}/inspection-photos")]
    [Authorize(Policy = Policies.RequireHub)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(26 * 1024 * 1024)]
    public async Task<IActionResult> UploadInspectionPhotos(Guid jobId, [FromForm] UploadInspectionPhotosRequest request, CancellationToken cancellationToken)
    {
        if (request.Photos is null || request.Photos.Count is < 1 or > 5)
            return BadRequest(new { error = new { code = "validation_error", message = "Tải lên từ 1 đến 5 ảnh QC." } });
        if (request.Photos.Any(photo =>
                photo.Length == 0 ||
                photo.Length > 5 * 1024 * 1024 ||
                !photo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new { error = new { code = "validation_error", message = "Mỗi ảnh QC phải là tệp ảnh không quá 5 MB." } });
        }

        var photoKeys = new List<string>();
        try
        {
            foreach (var photo in request.Photos)
            {
                var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
                var objectName = $"qc/{jobId}/{Guid.NewGuid()}{extension}";
                await using var stream = photo.OpenReadStream();
                await files.UploadAsync(_minio.PhotoBucket, objectName, stream, photo.ContentType, cancellationToken);
                photoKeys.Add(objectName);
            }
        }
        catch
        {
            // Best-effort cleanup so a half-finished batch does not leak objects in the bucket.
            foreach (var key in photoKeys)
                await files.DeleteAsync(_minio.PhotoBucket, key, cancellationToken);
            throw;
        }

        return Ok(new { photoKeys });
    }

    // UC-030 / FR-ANAL-007: quote and QC-proof review are responsibilities of
    // Operations Manager. Admin retains operational override access.
    [HttpGet("qc-proofs")]
    [Authorize(Policy = Policies.RequireQuoteReview)]
    public async Task<IActionResult> GetQcProofs(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetJobsQuery(JobStatus.AwaitingInspection), cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);

        var pending = result.Value.Where(j => j.QcProofStatus == "Pending").ToList();
        var response = new List<object>();
        foreach (var job in pending)
        {
            var urls = new List<string>();
            foreach (var key in job.QcProofPhotoKeys)
                urls.Add(await files.GetPresignedUrlAsync(_minio.PhotoBucket, key, TimeSpan.FromMinutes(15)));
            response.Add(new { Job = job, PhotoUrls = urls });
        }
        return Ok(response);
    }

    [HttpPost("{jobId:guid}/qc-proof/review")]
    [Authorize(Policy = Policies.RequireQuoteReview)]
    public async Task<IActionResult> ReviewQcProof(Guid jobId, [FromBody] ReviewQcProofRequest request, CancellationToken cancellationToken)
    {
        var staffId = User.GetCustomerId();
        if (staffId is null) return Forbid();
        var result = await sender.Send(
            new ReviewQcProofCommand(jobId, staffId.Value, request.Approved, request.Reason),
            cancellationToken);
        return ToResult(result);
    }

    [HttpPost("{jobId:guid}/inspect")]
    [Authorize(Policy = Policies.RequireHub)]
    public async Task<IActionResult> Inspect(Guid jobId, [FromBody] InspectRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new InspectJobCommand(
            jobId,
            request.Passed,
            request.ChecklistResults ?? [],
            request.PhotoUrls ?? [],
            request.FaultAttribution,
            request.Note,
            request.ChecklistVersionId,
            request.DefectCode), cancellationToken);
        return ToResult(result);
    }

    private IActionResult ToResult(Result result)
    {
        if (result.IsSuccess) return NoContent();
        var status = result.Error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
    }
}

public sealed class CompleteJobRequest(List<IFormFile>? photos, int actualPrintMinutes, decimal? actualMaterialGrams)
{
    public int ActualPrintMinutes { get; } = actualPrintMinutes;
    public static string SelfReport => string.Empty;
    public List<IFormFile>? Photos { get; } = photos;

    /// <summary>Material the lab actually used, for the stock ledger. Optional until the form collects it.</summary>
    public decimal? ActualMaterialGrams { get; } = actualMaterialGrams;
}
public sealed class UploadInspectionPhotosRequest(List<IFormFile>? photos)
{
    public List<IFormFile>? Photos { get; } = photos;
}

public record ReviewQcProofRequest(bool Approved, string? Reason);
public record InspectRequest(
    bool Passed,
    IReadOnlyList<ChecklistItemResult>? ChecklistResults = null,
    IReadOnlyList<string>? PhotoUrls = null,
    FaultAttribution? FaultAttribution = null,
    string? Note = null,
    Guid? ChecklistVersionId = null,
    string? DefectCode = null);
public record DeclineJobRequest(string Reason);
public record ReportJobFailureRequest(string Reason);
