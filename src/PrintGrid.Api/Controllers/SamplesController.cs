using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PrintGrid.Api.Authorization;
using PrintGrid.Api.Models;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Modules.Admin.Application.Samples;
using PrintGrid.Modules.Customer.Application.Models;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// Public sample library (FR-CUST-014). Reads are anonymous; "Đặt in" copies the sample
/// into the caller's own library and needs a Customer token.
/// </summary>
[ApiController]
[Route("api/v1/samples")]
public sealed class SamplesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IFileStorage _files;
    private readonly MinioOptions _minio;
    private readonly ModelUploadOptions _uploadOptions;

    public SamplesController(
        ISender sender,
        IFileStorage files,
        IOptions<MinioOptions> minio,
        IOptions<ModelUploadOptions> uploadOptions)
    {
        _sender = sender;
        _files = files;
        _minio = minio.Value;
        _uploadOptions = uploadOptions.Value;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetSamples(
        [FromQuery] string? search,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var samples = await _sender.Send(new GetPublicSamplesQuery(search, category), cancellationToken);
        return Ok(samples);
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSample(string slug, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPublicSampleQuery(slug), cancellationToken);
        return result.IsFailure ? NotFound(ErrorBody(result.Error)) : Ok(result.Value);
    }

    /// <summary>
    /// Streams the sample binary so the 3D viewer can render it without a token.
    /// Only published samples are reachable, and the URL never exposes storage keys.
    /// </summary>
    [HttpGet("{slug}/file")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadSampleFile(string slug, CancellationToken cancellationToken)
    {
        var source = await _sender.Send(new GetSampleSourceQuery(slug), cancellationToken);
        if (source.IsFailure) return NotFound(ErrorBody(source.Error));

        var storageKey = source.Value.StorageKey;
        var separator = storageKey.IndexOf('/');
        if (separator <= 0 || separator == storageKey.Length - 1)
            return NotFound(ErrorBody("file_not_stored", "Mẫu này chưa có file lưu trữ."));

        var stream = await _files.DownloadAsync(
            storageKey[..separator], storageKey[(separator + 1)..], cancellationToken);

        Response.Headers.CacheControl = "public,max-age=300";
        return File(stream, ContentTypeFor(Path.GetExtension(source.Value.FileName)), enableRangeProcessing: true);
    }

    /// <summary>
    /// "Đặt in mẫu này": copies the sample file into the customer's storage prefix and
    /// creates a private Model row carrying the sample's geometry. The platform sample is
    /// never mutated, and deleting the copy cannot delete the original.
    /// </summary>
    [HttpPost("{slug}/copy")]
    [Authorize(Policy = Policies.RequireCustomer)]
    public async Task<IActionResult> CopyToLibrary(string slug, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var source = await _sender.Send(new GetSampleSourceQuery(slug), cancellationToken);
        if (source.IsFailure) return NotFound(ErrorBody(source.Error));
        var sample = source.Value;

        if (sample.VolumeCm3 is null || sample.EstimatedPrintMinutes is null || sample.EstimatedMaterialGrams is null
            || sample.BoundingWidthMm is null || sample.BoundingDepthMm is null || sample.BoundingHeightMm is null)
            return Conflict(ErrorBody("sample_not_ready", "Mẫu này chưa có phân tích hình học. Hãy thử lại sau."));

        var quota = (await _sender.Send(new GetModelQuotaQuery(
            customerId.Value, _uploadOptions.MaxModelsPerCustomer, _uploadOptions.MaxStorageBytesPerCustomer),
            cancellationToken)).Value;

        if (quota.UsedModels >= quota.MaxModels || quota.UsedBytes + sample.SizeBytes > quota.MaxBytes)
            return Conflict(new
            {
                error = new
                {
                    code = "quota_exceeded",
                    message = "Đã đạt giới hạn lưu trữ. Hãy xóa model không còn sử dụng rồi thử lại.",
                    details = quota
                }
            });

        var separator = sample.StorageKey.IndexOf('/');
        if (separator <= 0 || separator == sample.StorageKey.Length - 1)
            return NotFound(ErrorBody("file_not_stored", "Mẫu này chưa có file lưu trữ."));

        var extension = Path.GetExtension(sample.FileName).ToLowerInvariant();
        var newObjectName = $"{customerId}/{Guid.NewGuid()}{extension}";

        await using var sourceStream = await _files.DownloadAsync(
            sample.StorageKey[..separator], sample.StorageKey[(separator + 1)..], cancellationToken);
        await using var buffer = new MemoryStream();
        await sourceStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(buffer, cancellationToken)).ToLowerInvariant();
        buffer.Position = 0;
        await _files.UploadAsync(_minio.ModelBucket, newObjectName, buffer, ContentTypeFor(extension), cancellationToken);

        try
        {
            var result = await _sender.Send(new CreateModelFromSampleCommand(
                customerId.Value,
                sample.Name,
                sample.Description,
                sample.FileName,
                sample.FileFormat,
                sample.SizeBytes,
                sample.Tags,
                $"{_minio.ModelBucket}/{newObjectName}",
                sha256,
                sample.BoundingWidthMm.Value,
                sample.BoundingDepthMm.Value,
                sample.BoundingHeightMm.Value,
                sample.VolumeCm3.Value,
                sample.EstimatedPrintMinutes.Value,
                sample.EstimatedMaterialGrams.Value),
                cancellationToken);

            if (result.IsFailure)
            {
                await _files.DeleteAsync(_minio.ModelBucket, newObjectName, CancellationToken.None);
                return BadRequest(ErrorBody(result.Error));
            }

            return Created($"/api/v1/models/{result.Value.Id}", result.Value);
        }
        catch
        {
            await _files.DeleteAsync(_minio.ModelBucket, newObjectName, CancellationToken.None);
            throw;
        }
    }

    private static object ErrorBody(PrintGrid.SharedKernel.Results.Error error) =>
        new { error = new { code = error.Code, message = error.Message } };

    private static object ErrorBody(string code, string message) => new { error = new { code, message } };

    private static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".stl" => "model/stl",
        ".obj" => "model/obj",
        ".3mf" => "model/3mf",
        ".glb" => "model/gltf-binary",
        _ => "application/octet-stream"
    };
}
