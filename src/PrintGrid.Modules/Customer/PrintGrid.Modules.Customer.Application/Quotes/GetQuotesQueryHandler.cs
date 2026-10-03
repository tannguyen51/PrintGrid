using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public class GetQuotesQueryHandler : IRequestHandler<GetQuotesQuery, Result<IReadOnlyList<QuoteDto>>>
{
    private readonly IQuoteRepository _quotes;

    public GetQuotesQueryHandler(IQuoteRepository quotes) => _quotes = quotes;

    public async Task<Result<IReadOnlyList<QuoteDto>>> Handle(GetQuotesQuery query, CancellationToken cancellationToken)
    {
        var quotes = await _quotes.GetByCustomerAsync(query.CustomerId, cancellationToken);
        var dtos = quotes
            .OrderByDescending(q => q.CreatedAt)
            .Select(QuoteMappings.ToDto)
            .ToList();
        return Result.Success<IReadOnlyList<QuoteDto>>(dtos);
    }
}