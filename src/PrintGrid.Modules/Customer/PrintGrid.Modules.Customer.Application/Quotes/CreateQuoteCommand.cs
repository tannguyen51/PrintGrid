using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Quotes;

/// <summary>
/// Command for creating a quotation. Extended with tech constraints and machine binding
/// per customer-machine-selection spec (sections 2–7). BR-03, BR-04, BR-05, BR-09.
/// </summary>
public record CreateQuoteCommand(
    Guid CustomerId,
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
    // Section 3: Tech constraints (hard filters, no surcharge) — BR-03
    int? MaxColors = null,
    bool? EnclosedChamber = null,
    decimal? NozzleDiameterMm = null,
    decimal? MaxToleranceMm = null,
    decimal? MinLayerHeightMm = null,
    // Section 4/5: Machine binding type — BR-04, BR-05, BR-09
    int? BindingType = null,   // 0=None, 1=ByModel, 2=LikeOldOrder
    Guid? PinnedModelId = null,
    bool HardRequirement = true,
    Guid? ReferenceLineId = null
) : IRequest<Result<QuoteDto>>;
