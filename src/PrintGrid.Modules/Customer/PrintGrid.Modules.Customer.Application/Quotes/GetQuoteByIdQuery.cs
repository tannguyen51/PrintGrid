using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public record GetQuoteByIdQuery(Guid CustomerId, Guid QuoteId) : IRequest<Result<QuoteDto>>;

public class GetQuoteByIdQueryHandler(IQuoteRepository quotes) : IRequestHandler<GetQuoteByIdQuery, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(GetQuoteByIdQuery query, CancellationToken cancellationToken)
    {
        var quote = await quotes.GetByIdAsync(query.QuoteId, cancellationToken);
        if (quote is null)
            return Result.Failure<QuoteDto>(Error.NotFound("Quote", query.QuoteId));
        return quote.CustomerId != query.CustomerId ? Result.Failure<QuoteDto>(Error.Forbidden("Quote belongs to another customer")) : Result.Success(QuoteMappings.ToCustomerDto(quote));
    }
}
