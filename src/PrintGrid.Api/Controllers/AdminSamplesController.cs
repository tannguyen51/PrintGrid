using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PrintGrid.Api.Authorization;
using PrintGrid.Api.Models;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using PrintGrid.Modules.Admin.Application.Samples;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// Admin curation of the public sample library (FR-CUST-014 · FR-ADMIN-003 pattern).
/// Only the Admin role can add, re-publish or remove samples; every write is audited.
/// </summary>
[ApiController]
[Route("api/v1/admin/samples")]
[Authorize(Policy = Policies.RequireAdmin)]
public sealed class AdminSamplesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IFileStorage _files;
    private readonly MinioOptions _minio;
    private readonly ModelUploadOptions _uploadOptions;
    private readonly ISlicingService _slicing;
    private readonly IAuditLogWriter _audit;

    public AdminSamplesController(
        ISender sender,
        IFileStorage files,
        IOptions<MinioOptions> minio,
        IOptions<ModelUploadOptions> uploadOptions,
        ISlicingService slicing,
        IAuditLogWriter audit)
    {
        _sender = sender;
        _files = files;
        _minio = minio.Value;
        _uploadOptions = uploadOptions.Value;
        _slicing = slicing;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetSamples(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetAdminSamplesQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSample(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSampleByIdQuery(id), cancellationToken);
        return result.IsFailure ? NotFound(ErrorBody(result.Error)) : Ok(result.Value);
    }

    /// <summary>Creates a sample from an uploaded model file. Geometry is analysed on the spot.</summary>
    [HttpPost]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] SaveSampleForm form, CancellationToken cancellationToken)
    {
        var upload = await PrepareUploadAsync(form.File, cancellationToken);
        if (upload.Error is not null) return BadRequest(ErrorBody(upload.ErrorCode!, upload.Error));

        try
        {
            var result = await _sender.Send(new CreateSampleCommand(
                form.Slug, form.Name, form.Description, form.Category ?? string.Empty,
                upload.FileName!, upload.FileFormat!, upload.SizeBytes!.Value,
                upload.StorageKey!, upload.Sha256, form.CoverImageUrl, SplitTags(form.Tags),
                form.SortOrder, form.IsPublished,
                upload.WidthMm!.Value, upload.DepthMm!.Value, upload.HeightMm!.Value, upload.VolumeCm3!.Value,
                upload.PrintMinutes!.Value, upload.MaterialGrams!.Value,
                ActorId()), cancellationToken);

            if (!result.IsSuccess)
            {
                await DeleteStoredAsync(upload.StorageKey);
                return Conflict(ErrorBody(result.ErrorCode!, result.ErrorMessage!));
            }

            await WriteAudit("sample.created", result.Value!.Id.ToString(), null, result.Value, cancellationToken);
            return CreatedAtAction(nameof(GetSample), new { id = result.Value.Id }, result.Value);
        }
        catch
        {
            await DeleteStoredAsync(upload.StorageKey);
            throw;
        }
    }

    /// <summary>Updates metadata; pass a file to replace the stored binary + geometry.</summary>
    [HttpPut("{id:guid}")]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> Update(Guid id, [FromForm] SaveSampleForm form, CancellationToken cancellationToken)
    {
        var before = await _sender.Send(new GetSampleByIdQuery(id), cancellationToken);
        if (before.IsFailure) return NotFound(ErrorBody(before.Error));

        PreparedUpload upload = new();
        if (form.File is { Length: > 0 })
        {
            upload = await PrepareUploadAsync(form.File, cancellationToken);
            if (upload.Error is not null) return BadRequest(ErrorBody(upload.ErrorCode!, upload.Error));
        }

        try
        {
            var result = await _sender.Send(new UpdateSampleCommand(
                id, form.Slug, form.Name, form.Description, form.Category ?? string.Empty,
                form.CoverImageUrl, SplitTags(form.Tags), form.SortOrder,
                upload.FileName, upload.FileFormat, upload.File is null ? null : upload.SizeBytes,
                upload.StorageKey, upload.Sha256,
                upload.WidthMm, upload.DepthMm, upload.HeightMm, upload.VolumeCm3,
                upload.PrintMinutes, upload.MaterialGrams,
                ActorId()), cancellationToken);

            if (!result.IsSuccess)
            {
                await DeleteStoredAsync(upload.StorageKey);
                return Conflict(ErrorBody(result.ErrorCode!, result.ErrorMessage!));
            }

            // Only after the row is saved: drop the previous binary so storage never leaks.
            if (upload.StorageKey is not null && upload.StorageKey != before.Value.StorageKey)
                await DeleteStoredAsync(before.Value.StorageKey);

            await WriteAudit("sample.updated", id.ToString(), before.Value, result.Value, cancellationToken);
            return Ok(result.Value);
        }
        catch
        {
            await DeleteStoredAsync(upload.StorageKey);
            throw;
        }
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> SetPublished(Guid id, [FromBody] SetSamplePublishedRequest request, CancellationToken cancellationToken)
    {
        var before = await _sender.Send(new GetSampleByIdQuery(id), cancellationToken);
        if (before.IsFailure) return NotFound(ErrorBody(before.Error));

        var result = await _sender.Send(new SetSamplePublishedCommand(id, request.Published), cancellationToken);
        if (!result.IsSuccess) return Conflict(ErrorBody(result.ErrorCode!, result.ErrorMessage!));

        await WriteAudit(
            request.Published ? "sample.published" : "sample.unpublished",
            id.ToString(), before.Value, result.Value, cancellationToken);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var before = await _sender.Send(new GetSampleByIdQuery(id), cancellationToken);
        if (before.IsFailure) return NotFound(ErrorBody(before.Error));

        var result = await _sender.Send(new DeleteSampleCommand(id), cancellationToken);
        if (!result.IsSuccess) return Conflict(ErrorBody(result.ErrorCode!, result.ErrorMessage!));

        await DeleteStoredAsync(before.Value.StorageKey);
        await WriteAudit("sample.deleted", id.ToString(), before.Value, null, cancellationToken);
        return NoContent();
    }

    // ── upload pipeline ──────────────────────────────────────────────────────────

    private sealed class PreparedUpload
    {
        public string? Error { get; init; }
        public string? ErrorCode { get; init; }
        public IFormFile? File { get; init; }
        public string? FileName { get; init; }
        public string? FileFormat { get; init; }
        public long? SizeBytes { get; init; }
        public string? StorageKey { get; init; }
        public string? Sha256 { get; init; }
        public decimal? WidthMm { get; init; }
        public decimal? DepthMm { get; init; }
        public decimal? HeightMm { get; init; }
        public decimal? VolumeCm3 { get; init; }
        public int? PrintMinutes { get; init; }
        public decimal? MaterialGrams { get; init; }
    }

    private async Task<PreparedUpload> PrepareUploadAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return new PreparedUpload { ErrorCode = "file_required", Error = "Hãy chọn file mô hình (STL, OBJ, 3MF hoặc GLB)." };

        if (file.Length > _uploadOptions.MaxFileBytes)
            return new PreparedUpload { ErrorCode = "file_too_large", Error = "Kích thước file vượt giới hạn 50 MiB." };

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ModelFileInspector.IsAllowedExtension(extension))
            return new PreparedUpload
            {
                ErrorCode = "unsupported_format",
                Error = "Định dạng không được hỗ trợ. Chỉ chấp nhận STL, OBJ, 3MF hoặc GLB."
            };

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var analysis = await _slicing.AnalyzeAsync(buffer, extension, cancellationToken);
        if (!analysis.IsValid)
            return new PreparedUpload
            {
                ErrorCode = "invalid_file_content",
                Error = analysis.ErrorMessage ?? "Không đọc được hình học của file."
            };

        var estimate = _slicing.Estimate(analysis, "PLA", 0.2m, 30, PrintTechnology.Fdm);

        var sha256 = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(buffer, cancellationToken))
            .ToLowerInvariant();

        var objectName = $"samples/{Guid.NewGuid()}{extension}";
        buffer.Position = 0;
        await _files.UploadAsync(_minio.ModelBucket, objectName, buffer, ContentTypeFor(extension), cancellationToken);

        return new PreparedUpload
        {
            File = file,
            FileName = file.FileName,
            FileFormat = extension.TrimStart('.'),
            SizeBytes = file.Length,
            StorageKey = $"{_minio.ModelBucket}/{objectName}",
            Sha256 = sha256,
            WidthMm = analysis.WidthMm,
            DepthMm = analysis.DepthMm,
            HeightMm = analysis.HeightMm,
            VolumeCm3 = analysis.VolumeCm3,
            PrintMinutes = estimate.PrintMinutes,
            MaterialGrams = estimate.MaterialGrams
        };
    }

    private async Task DeleteStoredAsync(string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return;
        var separator = storageKey.IndexOf('/');
        if (separator <= 0 || separator == storageKey.Length - 1) return;
        await _files.DeleteAsync(storageKey[..separator], storageKey[(separator + 1)..], CancellationToken.None);
    }

    private static IReadOnlyList<string>? SplitTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? null
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".stl" => "model/stl",
        ".obj" => "model/obj",
        ".3mf" => "model/3mf",
        ".glb" => "model/gltf-binary",
        _ => "application/octet-stream"
    };

    private Guid ActorId() => Guid.Parse(User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Task WriteAudit(string action, string entityId, object? before, object? after, CancellationToken cancellationToken) =>
        _audit.WriteAsync(new AuditLogWriteRequest(
            ActorId(),
            User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "unknown",
            User.FindFirstValue(ClaimTypes.Role) ?? "Admin",
            AuditActionClasses.Configuration,
            action,
            "SampleModel",
            entityId,
            before,
            after,
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            TraceId: HttpContext.TraceIdentifier,
            RequirementCodes: ["FR-CUST-014", "FR-ADMIN-003"]), cancellationToken);

    private static object ErrorBody(PrintGrid.SharedKernel.Results.Error error) =>
        new { error = new { code = error.Code, message = error.Message } };

    private static object ErrorBody(string code, string message) => new { error = new { code, message } };
}

public sealed record SaveSampleForm(
    IFormFile? File,
    string Name,
    string? Slug,
    string? Description,
    string? Category,
    string? Tags,
    string? CoverImageUrl,
    int SortOrder = 0,
    bool IsPublished = false);

public sealed record SetSamplePublishedRequest(bool Published);
