using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Quotes;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/quotes")]
[Authorize(Policy = Policies.RequireCustomer)]
public class QuotesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQuotes(CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await sender.Send(new GetQuotesQuery(customerId.Value), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpGet("{quoteId:guid}")]
    public async Task<IActionResult> GetQuote(Guid quoteId, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await sender.Send(new GetQuoteByIdQuery(customerId.Value, quoteId), cancellationToken);
        return result.IsFailure
            ? NotFound(new { error = new { code = result.Error.Code, message = result.Error.Message } })
            : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuoteRequest request, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await sender.Send(
            new CreateQuoteCommand(
                customerId.Value,
                request.ModelId,
                request.MaterialCode,
                request.ColorCode,
                request.LayerHeightMm,
                request.InfillPercent,
                request.Quantity,
                request.ToleranceMm, request.SizeCode ?? "ORIGINAL", request.ColorCodes, request.Polishing,
                request.MaxColors, request.EnclosedChamber, request.NozzleDiameterMm, request.MaxToleranceMm, request.MinLayerHeightMm,
                request.BindingType, request.PinnedMachineId, true, request.PreviousOrderLineId),
            cancellationToken);

        if (!result.IsFailure) return CreatedAtAction(nameof(GetQuotes), new { }, result.Value);
        var status = result.Error.Code == "conflict"
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status400BadRequest;
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });

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
    bool Polishing = false,
    // BR-03: Tech constraints (hard filters, no surcharge)
    int? MaxColors = null,
    bool? EnclosedChamber = null,
    decimal? NozzleDiameterMm = null,
    decimal? MaxToleranceMm = null,
    decimal? MinLayerHeightMm = null,
    // Section 4/5: Machine binding type — BR-04, BR-05, BR-09
    int? BindingType = null,   // 0=None, 1=ByModel, 2=LikeOldOrder
    Guid? PinnedMachineId = null,
    Guid? PreviousOrderLineId = null);
