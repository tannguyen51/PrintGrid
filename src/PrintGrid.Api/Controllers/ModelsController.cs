using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Models;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/models")]
[Authorize(Policy = Policies.RequireCustomer)]
public class ModelsController : ControllerBase
{
    private readonly ISender _sender;

    public ModelsController(ISender sender) => _sender = sender;

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