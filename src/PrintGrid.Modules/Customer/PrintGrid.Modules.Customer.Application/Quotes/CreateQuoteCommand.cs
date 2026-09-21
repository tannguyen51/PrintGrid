using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public record CreateQuoteCommand(
    Guid CustomerId,
    Guid ModelId,
    string MaterialCode,
    string ColorCode,
    decimal LayerHeightMm,
    int InfillPercent,
    int Quantity,
    decimal ToleranceMm) : IRequest<Result<QuoteDto>>;