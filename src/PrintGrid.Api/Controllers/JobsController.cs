using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;
using PrintGrid.Modules.Scheduling.Application.Queries;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Infrastructure.Shared.FileStorage;
using Microsoft.Extensions.Options;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IFileStorage _files;
    private readonly MinioOptions _minio;

    public JobsController(ISender sender, IFileStorage files, IOptions<MinioOptions> minio)
    {
        _sender = sender;
        _files = files;
        _minio = minio.Value;
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireProduction)]
    // Lab/Hub/Ops each read the job board — they filter by status in the UI.
    public async Task<IActionResult> GetJobs([FromQuery] JobStatus? status, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetJobsQuery(status ?? JobStatus.Pending), cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        var jobs = result.Value;
        if (status == JobStatus.AwaitingInspection &&
            (User.IsInRole(Roles.HubQc) || User.IsInRole(Roles.HubFulfillment)))
            jobs = jobs.Where(j => j.QcProofStatus == "Approved").ToList();
        return Ok(jobs);
    }

    [HttpPost("{jobId:guid}/accept")]
    [Authorize(Policy = Policies.RequireLab)]
    public async Task<IActionResult> Accept(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AcceptJobCommand(jobId), cancellationToken);
        return ToResult(result);
    }

    [HttpPost("{jobId:guid}/decline")]
    [Authorize(Policy = Policies.RequireLab)]
    public async Task<IActionResult> Decline(Guid jobId, [FromBody] DeclineJobRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeclineJobCommand(jobId, request.Reason), cancellationToken);
        return ToResult(result);
    }

    [HttpPost("{jobId:guid}/start")]
    [Authorize(Policy = Policies.RequireLab)]
    public async Task<IActionResult> Start(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new StartJobCommand(jobId), cancellationToken);
        return ToResult(result);
    }

    [HttpPost("{jobId:guid}/complete")]
    [Authorize(Policy = Policies.RequireLab)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(26 * 1024 * 1024)]
    public async Task<IActionResult> Complete(Guid jobId, [FromForm] CompleteJobRequest request, CancellationToken cancellationToken)
    {
        if (request.Photos is null || request.Photos.Count is < 1 or > 5)
            return BadRequest(new { error = new { code = "validation_error", message = "Tải lên từ 1 đến 5 ảnh QC." } });
        if (request.Photos.Any(photo =>
                photo.Length == 0 ||
                photo.Length > 5 * 1024 * 1024 ||
                !photo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { error = new { code = "validation_error", message = "Mỗi ảnh QC phải là tệp ảnh không quá 5 MB." } });

        var photoKeys = new List<string>();
        foreach (var photo in request.Photos)
        {
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var objectName = $"qc/{jobId}/{Guid.NewGuid()}{extension}";
            await using var stream = photo.OpenReadStream();
            await _files.UploadAsync(_minio.PhotoBucket, objectName, stream, photo.ContentType, cancellationToken);
            photoKeys.Add(objectName);
        }

        var result = await _sender.Send(
            new CompleteJobCommand(jobId, request.ActualPrintMinutes, request.SelfReport, photoKeys, request.ActualMaterialGrams),
            cancellationToken);
        if (result.IsFailure)
        {
            foreach (var key in photoKeys)
                await _files.DeleteAsync(_minio.PhotoBucket, key, cancellationToken);
        }
        return ToResult(result);
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
            return BadRequest(new { error = new { code = "validation_error", message = "Mỗi ảnh QC phải là tệp ảnh không quá 5 MB." } });

        var photoKeys = new List<string>();
        try
        {
            foreach (var photo in request.Photos)
            {
                var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
                var objectName = $"qc/{jobId}/{Guid.NewGuid()}{extension}";
                await using var stream = photo.OpenReadStream();
                await _files.UploadAsync(_minio.PhotoBucket, objectName, stream, photo.ContentType, cancellationToken);
                photoKeys.Add(objectName);
            }
        }
        catch
        {
            // Best-effort cleanup so a half-finished batch does not leak objects in the bucket.
            foreach (var key in photoKeys)
                await _files.DeleteAsync(_minio.PhotoBucket, key, cancellationToken);
            throw;
        }

        return Ok(new { photoKeys });
    }

    [HttpGet("qc-proofs")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> GetQcProofs(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetJobsQuery(JobStatus.AwaitingInspection), cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);

        var pending = result.Value.Where(j => j.QcProofStatus == "Pending").ToList();
        var response = new List<object>();
        foreach (var job in pending)
        {
            var urls = new List<string>();
            foreach (var key in job.QcProofPhotoKeys)
                urls.Add(await _files.GetPresignedUrlAsync(_minio.PhotoBucket, key, TimeSpan.FromMinutes(15)));
            response.Add(new { Job = job, PhotoUrls = urls });
        }
        return Ok(response);
    }

    [HttpPost("{jobId:guid}/qc-proof/review")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> ReviewQcProof(Guid jobId, [FromBody] ReviewQcProofRequest request, CancellationToken cancellationToken)
    {
        var staffId = User.GetCustomerId();
        if (staffId is null) return Forbid();
        var result = await _sender.Send(
            new ReviewQcProofCommand(jobId, staffId.Value, request.Approved, request.Reason),
            cancellationToken);
        return ToResult(result);
    }

    [HttpPost("{jobId:guid}/inspect")]
    [Authorize(Policy = Policies.RequireHub)]
    public async Task<IActionResult> Inspect(Guid jobId, [FromBody] InspectRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new InspectJobCommand(
            jobId,
            request.Passed,
            request.ChecklistResults ?? Array.Empty<ChecklistItemResult>(),
            request.PhotoUrls ?? Array.Empty<string>(),
            request.FaultAttribution,
            request.Note), cancellationToken);
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

public sealed class CompleteJobRequest
{
    public int ActualPrintMinutes { get; init; }
    public string SelfReport { get; init; } = string.Empty;
    public List<IFormFile>? Photos { get; init; }

    /// <summary>Material the lab actually used, for the stock ledger. Optional until the form collects it.</summary>
    public decimal? ActualMaterialGrams { get; init; }
}
public sealed class UploadInspectionPhotosRequest
{
    public List<IFormFile>? Photos { get; init; }
}

public record ReviewQcProofRequest(bool Approved, string? Reason);
public record InspectRequest(
    bool Passed,
    IReadOnlyList<ChecklistItemResult>? ChecklistResults = null,
    IReadOnlyList<string>? PhotoUrls = null,
    FaultAttribution? FaultAttribution = null,
    string? Note = null);
public record DeclineJobRequest(string Reason);
