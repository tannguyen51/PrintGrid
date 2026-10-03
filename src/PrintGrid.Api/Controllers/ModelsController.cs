using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PrintGrid.Api.Authorization;
using PrintGrid.Api.BackgroundJobs;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Modules.Customer.Application.Models;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/models")]
[Authorize(Policy = Policies.RequireCustomer)]
public class ModelsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IFileStorage _files;
    private readonly MinioOptions _minio;

    public ModelsController(ISender sender, IFileStorage files, IOptions<MinioOptions> minio)
    {
        _sender = sender;
        _files = files;
        _minio = minio.Value;
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

        var result = await _sender.Send(new DeleteModelCommand(customerId.Value, modelId), cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        return NoContent();
    }

    [HttpPost("upload")]
    [RequestSizeLimit(60 * 1024 * 1024)] // 60 MB cap (FR-CUST-002 allows 50 MB per file)
    public async Task<IActionResult> Upload(
        [FromForm] UploadModelRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        if (request.File is null || request.File.Length == 0)
            return BadRequest(new { error = new { code = "validation_error", message = "File is required" } });

        await using var stream = request.File.OpenReadStream();

        // Object key: printgrid-models/{customerId}/{guid}.{ext}
        var ext = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        var randomId = Guid.NewGuid();
        var objectName = $"{customerId}/{randomId}{ext}";
        await _files.UploadAsync(_minio.ModelBucket, objectName, stream, request.File.ContentType, cancellationToken);

        var result = await _sender.Send(
            new UploadModelCommand(
                customerId.Value,
                request.Name,
                request.Description,
                request.File.FileName,
                ext.TrimStart('.'),
                request.File.Length,
                request.Tags,
                $"{_minio.ModelBucket}/{objectName}"),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        // Async geometry analysis + reference print-time estimate (FR-SCHED-001/002).
        BackgroundJob.Enqueue<AnalyzeModelJob>(job => job.ExecuteAsync(result.Value.Id, CancellationToken.None));

        return CreatedAtAction(nameof(GetModel), new { modelId = result.Value.Id }, result.Value);
    }
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