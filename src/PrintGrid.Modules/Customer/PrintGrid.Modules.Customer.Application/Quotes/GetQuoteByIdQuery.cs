using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public record GetQuoteByIdQuery(Guid CustomerId, Guid QuoteId) : IRequest<Result<QuoteDto>>;

public class GetQuoteByIdQueryHandler : IRequestHandler<GetQuoteByIdQuery, Result<QuoteDto>>
{
    private readonly IQuoteRepository _quotes;

    public GetQuoteByIdQueryHandler(IQuoteRepository quotes) => _quotes = quotes;

    public async Task<Result<QuoteDto>> Handle(GetQuoteByIdQuery query, CancellationToken cancellationToken)
    {
        var quote = await _quotes.GetByIdAsync(query.QuoteId, cancellationToken);
        if (quote is null)
            return Result.Failure<QuoteDto>(Error.NotFound("Quote", query.QuoteId));
        if (quote.CustomerId != query.CustomerId)
            return Result.Failure<QuoteDto>(Error.Forbidden("Quote belongs to another customer"));

        return Result.Success(QuoteMappings.ToDto(quote));
    }
}
