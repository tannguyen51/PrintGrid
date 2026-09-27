using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public record GetQuotesQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<QuoteDto>>>;