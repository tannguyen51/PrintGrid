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
                request.ToleranceMm),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = new { code = result.Error.Code, message = result.Error.Message } });

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
    decimal ToleranceMm);