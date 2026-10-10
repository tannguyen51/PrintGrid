using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using PrintGrid.Api.Authorization;
using PrintGrid.Api.BackgroundJobs;
using PrintGrid.Api.Models;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Modules.Customer.Application.Models;
using PrintGrid.Modules.Scheduling.Application.Abstractions;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/models")]
[Authorize(Policy = Policies.RequireCustomer)]
public class ModelsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IFileStorage _files;
    private readonly MinioOptions _minio;
    private readonly ModelUploadOptions _uploadOptions;
    private readonly ISlicingService _slicingService;
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> UploadLocks = new();

    public ModelsController(
        ISender sender,
        IFileStorage files,
        IOptions<MinioOptions> minio,
        IOptions<ModelUploadOptions> uploadOptions,
        ISlicingService slicingService)
    {
        _sender = sender;
        _files = files;
        _minio = minio.Value;
        _uploadOptions = uploadOptions.Value;
        _slicingService = slicingService;
    }

    [HttpGet("quota")]
    public async Task<IActionResult> GetQuota(CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        var result = await GetQuotaAsync(customerId.Value, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetModels([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await _sender.Send(new GetModelsQuery(customerId.Value, search), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpGet("{modelId:guid}")]
    public async Task<IActionResult> GetModel(Guid modelId, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await _sender.Send(new GetModelQuery(customerId.Value, modelId), cancellationToken);
        return result.IsFailure
            ? NotFound(new { error = new { code = result.Error.Code, message = result.Error.Message } })
            : Ok(result.Value);
    }

    /// <summary>
    /// Streams the stored model file so the browser can preview it in the 3D viewer.
    /// Ownership comes from GetModelQuery — a model you do not own is a 404, never a 403
    /// (which would confirm the model exists).
    /// </summary>
    [HttpGet("{modelId:guid}/file")]
    public async Task<IActionResult> DownloadModelFile(Guid modelId, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await _sender.Send(new GetModelQuery(customerId.Value, modelId), cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        var storageKey = result.Value.StorageKey;
        if (string.IsNullOrWhiteSpace(storageKey))
            return NotFound(new { error = new { code = "file_not_stored", message = "Model này chưa có file lưu trữ." } });

        // StorageKey is "{bucket}/{objectName}" (see Upload) — same split as AnalyzeModelJob.
        var separator = storageKey.IndexOf('/');
        if (separator <= 0 || separator == storageKey.Length - 1)
            return NotFound(new { error = new { code = "file_not_stored", message = "Đường dẫn file lưu trữ không hợp lệ." } });

        var stream = await _files.DownloadAsync(
            storageKey[..separator], storageKey[(separator + 1)..], cancellationToken);

        return File(stream, ContentTypeFor(Path.GetExtension(storageKey)), enableRangeProcessing: true);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateModelRequest request, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await _sender.Send(
            new CreateModelCommand(
                customerId.Value,
                request.Name,
                request.Description,
                request.FileName,
                request.FileFormat,
                request.SizeBytes,
                request.Tags),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        // Kick off async geometry analysis + estimate (FR-SCHED-001/002); the model stays
        // "Pending" until the job records the result the customer sees in the library.
        BackgroundJob.Enqueue<AnalyzeModelJob>(job => job.ExecuteAsync(result.Value.Id, CancellationToken.None));

        return CreatedAtAction(nameof(GetModel), new { modelId = result.Value.Id }, result.Value);
    }

    [HttpPut("{modelId:guid}")]
    public async Task<IActionResult> Update(Guid modelId, [FromBody] UpdateModelRequest request, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await _sender.Send(
            new UpdateModelCommand(
                customerId.Value,
                modelId,
                request.Name,
                request.Description,
                request.FileName,
                request.FileFormat,
                request.SizeBytes,
                request.Tags),
            cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code == "not_found"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        return Ok(result.Value);
    }

    [HttpDelete("{modelId:guid}")]
    public async Task<IActionResult> Delete(Guid modelId, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var existing = await _sender.Send(new GetModelQuery(customerId.Value, modelId), cancellationToken);
        if (existing.IsFailure)
            return NotFound(new { error = new { code = existing.Error.Code, message = existing.Error.Message } });

        var result = await _sender.Send(new DeleteModelCommand(customerId.Value, modelId), cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        if (!string.IsNullOrWhiteSpace(existing.Value.StorageKey))
        {
            var separator = existing.Value.StorageKey.IndexOf('/');
            if (separator > 0 && separator < existing.Value.StorageKey.Length - 1)
                await _files.DeleteAsync(
                    existing.Value.StorageKey[..separator],
                    existing.Value.StorageKey[(separator + 1)..],
                    cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("upload")]
    // Must stay ABOVE nginx's client_max_body_size (60m) and above the real per-file limit:
    // if Kestrel rejects the body while reading the multipart form, model binding fails first
    // and the caller gets a raw 400 instead of this endpoint's `file_too_large` message.
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadModelRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        if (request.File is null || request.File.Length == 0)
            return BadRequest(new { error = new { code = "validation_error", message = "File is required" } });

        if (request.File.Length > _uploadOptions.MaxFileBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                error = new { code = "file_too_large", message = "Kích thước file vượt giới hạn 50 MiB.", maxBytes = _uploadOptions.MaxFileBytes }
            });

        var ext = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (!ModelFileInspector.IsAllowedExtension(ext))
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new
            {
                error = new { code = "unsupported_format", message = "Định dạng không được hỗ trợ. Chỉ chấp nhận STL, OBJ, 3MF hoặc GLB." }
            });

        var uploadLock = UploadLocks.GetOrAdd(customerId.Value, _ => new SemaphoreSlim(1, 1));
        await uploadLock.WaitAsync(cancellationToken);
        try
        {
            var quota = await GetQuotaAsync(customerId.Value, cancellationToken);
            if (quota.UsedModels >= quota.MaxModels || quota.UsedBytes + request.File.Length > quota.MaxBytes)
                return Conflict(new
                {
                    error = new
                    {
                        code = "quota_exceeded",
                        message = "Đã đạt giới hạn lưu trữ. Hãy xóa model không còn sử dụng rồi thử lại.",
                        details = quota
                    }
                });

            await using var stream = request.File.OpenReadStream();

            // Compute SHA-256 of the exact bytes received (BR-IP-001) while buffering.
            using var buffer = new MemoryStream();
            using var sha = System.Security.Cryptography.SHA256.Create();
            await stream.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            var contentError = await ModelFileInspector.ValidateContentAsync(buffer, ext, _slicingService, cancellationToken);
            if (contentError is not null)
                return StatusCode(StatusCodes.Status415UnsupportedMediaType, new
                {
                    error = new { code = "invalid_file_content", message = contentError }
                });

            var sha256 = Convert.ToHexString(await sha.ComputeHashAsync(buffer, cancellationToken)).ToLowerInvariant();
            buffer.Position = 0;

            // Object key: printgrid-models/{customerId}/{guid}.{ext}
            var randomId = Guid.NewGuid();
            var objectName = $"{customerId}/{randomId}{ext}";
            await _files.UploadAsync(_minio.ModelBucket, objectName, buffer, ContentTypeFor(ext), cancellationToken);

            try
            {
                var result = await _sender.Send(
                    new UploadModelCommand(
                        customerId.Value,
                        request.Name,
                        request.Description,
                        request.File.FileName,
                        ext.TrimStart('.'),
                        request.File.Length,
                        request.Tags,
                        $"{_minio.ModelBucket}/{objectName}",
                        sha256),
                    cancellationToken);

                if (result.IsFailure)
                {
                    await _files.DeleteAsync(_minio.ModelBucket, objectName, cancellationToken);
                    return BadRequest(new { error = new { code = result.Error.Code, message = result.Error.Message } });
                }

                BackgroundJob.Enqueue<AnalyzeModelJob>(job => job.ExecuteAsync(result.Value.Id, CancellationToken.None));
                return CreatedAtAction(nameof(GetModel), new { modelId = result.Value.Id }, result.Value);
            }
            catch
            {
                await _files.DeleteAsync(_minio.ModelBucket, objectName, CancellationToken.None);
                throw;
            }
        }
        finally
        {
            uploadLock.Release();
        }
    }

    private async Task<ModelQuotaDto> GetQuotaAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetModelQuotaQuery(
            customerId,
            _uploadOptions.MaxModelsPerCustomer,
            _uploadOptions.MaxStorageBytesPerCustomer), cancellationToken);
        return result.Value;
    }

    private static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".stl" => "model/stl",
        ".obj" => "model/obj",
        ".3mf" => "model/3mf",
        ".glb" => "model/gltf-binary",
        _ => "application/octet-stream"
    };
}

public record CreateModelRequest(
    string Name,
    string? Description,
    string FileName,
    string FileFormat,
    long SizeBytes,
    IReadOnlyList<string>? Tags);

public record UpdateModelRequest(
    string Name,
    string? Description,
    string FileName,
    string FileFormat,
    long SizeBytes,
    IReadOnlyList<string>? Tags);

public record UploadModelRequest(
    string Name,
    string? Description,
    IFormFile? File,
    IReadOnlyList<string>? Tags);
