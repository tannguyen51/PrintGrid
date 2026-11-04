using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Quotes;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/quotes")]
[Authorize(Policy = Policies.RequireCustomer)]
public class QuotesController : ControllerBase
{
    private readonly ISender _sender;

    public QuotesController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> GetQuotes(CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await _sender.Send(new GetQuotesQuery(customerId.Value), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpGet("{quoteId:guid}")]
    public async Task<IActionResult> GetQuote(Guid quoteId, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await _sender.Send(new GetQuoteByIdQuery(customerId.Value, quoteId), cancellationToken);
        return result.IsFailure
            ? NotFound(new { error = new { code = result.Error.Code, message = result.Error.Message } })
            : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuoteRequest request, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await _sender.Send(
            new CreateQuoteCommand(
                customerId.Value,
                request.ModelId,
                request.MaterialCode,
                request.ColorCode,
                request.LayerHeightMm,
                request.InfillPercent,
                request.Quantity,
                request.ToleranceMm, request.SizeCode ?? "ORIGINAL", request.ColorCodes, request.Polishing),
            cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code == "conflict"
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        return CreatedAtAction(nameof(GetQuotes), new { }, result.Value);
    }
}

public record CreateQuoteRequest(
    Guid ModelId,
    string MaterialCode,
    string ColorCode,
    decimal LayerHeightMm,
    int InfillPercent,
    int Quantity,
    decimal ToleranceMm,
    string? SizeCode = null,
    IReadOnlyList<string>? ColorCodes = null,
    bool Polishing = false);
