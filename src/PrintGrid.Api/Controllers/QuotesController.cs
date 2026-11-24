using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Application.Queries.Machines;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/quotes")]
[Authorize(Policy = Policies.RequireCustomer)]
public class QuotesController(ISender sender) : ControllerBase
{
    [HttpGet("models")]
    public async Task<IActionResult> Models([FromQuery] MachineModelOptionsRequest request, CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        var result = await sender.Send(new GetEligibleMachineModelsQuery(new CreateQuoteCommand(
            customerId.Value, request.ModelId, request.MaterialCode, request.ColorCodes.FirstOrDefault() ?? "BLACK",
            request.LayerHeightMm, 20, 1, request.ToleranceMm, request.SizeCode, request.ColorCodes,
            MaxColors: request.MaxColors, EnclosedChamber: request.EnclosedChamber,
            NozzleDiameterMm: request.NozzleDiameterMm, MaxToleranceMm: request.MaxToleranceMm,
            MinLayerHeightMm: request.MinLayerHeightMm)), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] Guid modelId, [FromQuery] string? status,
        [FromQuery] string? search, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] string sort = "newest", [FromQuery] int page = 1, [FromQuery] int pageSize = 5,
        CancellationToken ct = default)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        PrintGrid.Modules.Customer.Domain.Enums.QuoteStatus? filterStatus = null;
        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            if (!Enum.TryParse<PrintGrid.Modules.Customer.Domain.Enums.QuoteStatus>(status, true, out var parsed) || !Enum.IsDefined(parsed))
                return BadRequest(new { error = new { code = "validation", message = "Trạng thái báo giá không hợp lệ." } });
            filterStatus = parsed;
        }
        var result = await sender.Send(new GetQuoteHistoryQuery(customerId.Value, modelId, filterStatus, search, from, to, sort, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
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
        if (request.PinnedMachineId.HasValue)
            return BadRequest(new { error = new { code = "validation", message = "Khách chỉ được chọn model máy, không được chỉ định một máy cụ thể." } });

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
                request.BindingType, request.PinnedModelId, true, request.PreviousOrderLineId, request.ColorAssignments),
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
    Guid? PreviousOrderLineId = null,
    Guid? PinnedModelId = null,
    IReadOnlyList<ColorGroupAssignment>? ColorAssignments = null);

public record MachineModelOptionsRequest(Guid ModelId, string MaterialCode, string[] ColorCodes,
    decimal LayerHeightMm = .2m, decimal ToleranceMm = .2m, string SizeCode = "ORIGINAL",
    int? MaxColors = null, bool? EnclosedChamber = null, decimal? NozzleDiameterMm = null,
    decimal? MaxToleranceMm = null, decimal? MinLayerHeightMm = null);
