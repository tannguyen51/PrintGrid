using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PrintGrid.Api.Authorization;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Modules.Customer.Application.Commands.DesignRequests;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// FR-CUST-013 / BR-IP-004 — design intake. Customers submit a brief with 1–5 reference images
/// (uploaded through the MinIO photo bucket, same contract as QC photos); ops triages the queue.
/// </summary>
[ApiController]
[Route("api/v1/design-requests")]
public sealed class DesignRequestsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IFileStorage _files;
    private readonly MinioOptions _minio;

    public DesignRequestsController(ISender sender, IFileStorage files, IOptions<MinioOptions> minio)
    {
        _sender = sender;
        _files = files;
        _minio = minio.Value;
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireCustomer)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDesignRequestBody body,
        CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await _sender.Send(
            new CreateDesignRequestCommand(
                customerId.Value,
                body.Title,
                body.Description,
                body.ReferenceImageKeys ?? Array.Empty<string>()),
            cancellationToken);

        return result.IsFailure
            ? ToError(result.Error)
            : CreatedAtAction(nameof(GetMine), null, result.Value);
    }

    [HttpGet("mine")]
    [Authorize(Policy = Policies.RequireCustomer)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await _sender.Send(new GetMyDesignRequestsQuery(customerId.Value), cancellationToken);
        return result.IsFailure ? ToError(result.Error) : Ok(result.Value);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> Get([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDesignRequestsQuery(status), cancellationToken);
        return result.IsFailure ? ToError(result.Error) : Ok(result.Value);
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeDesignRequestStatusBody body,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ChangeDesignRequestStatusCommand(id, body.Status), cancellationToken);
        return result.IsFailure ? ToError(result.Error) : Ok(result.Value);
    }

    /// <summary>
    /// Uploads 1–5 reference images to the photo bucket and returns their object keys; the keys are
    /// then sent with <see cref="Create"/>. Mirrors the QC inspection-photo endpoint.
    /// </summary>
    [HttpPost("images")]
    [Authorize(Policy = Policies.RequireCustomer)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(26 * 1024 * 1024)]
    public async Task<IActionResult> UploadImages(
        [FromForm] UploadDesignImagesRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        if (request.Images is null || request.Images.Count is < 1 or > 5)
            return BadRequest(new { error = new { code = "validation_error", message = "Tải lên từ 1 đến 5 ảnh tham chiếu." } });
        if (request.Images.Any(image =>
                image.Length == 0 ||
                image.Length > 5 * 1024 * 1024 ||
                !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new { error = new { code = "validation_error", message = "Mỗi ảnh tham chiếu phải là tệp ảnh không quá 5 MB." } });
        }

        var imageKeys = new List<string>();
        try
        {
            foreach (var image in request.Images)
            {
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                var objectName = $"design/{customerId.Value}/{Guid.NewGuid():N}{extension}";
                await using var stream = image.OpenReadStream();
                await _files.UploadAsync(_minio.PhotoBucket, objectName, stream, image.ContentType, cancellationToken);
                imageKeys.Add(objectName);
            }
        }
        catch
        {
            // Best-effort cleanup so a half-finished batch does not leak objects in the bucket.
            foreach (var key in imageKeys)
                await _files.DeleteAsync(_minio.PhotoBucket, key, cancellationToken);
            throw;
        }

        return Ok(new { imageKeys });
    }

    private IActionResult ToError(Error error)
    {
        var status = error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, new { error = new { code = error.Code, message = error.Message } });
    }
}

public sealed record CreateDesignRequestBody(
    string Title,
    string Description,
    IReadOnlyList<string>? ReferenceImageKeys = null);

public sealed record ChangeDesignRequestStatusBody(string Status);

public sealed class UploadDesignImagesRequest
{
    public List<IFormFile>? Images { get; init; }
}
