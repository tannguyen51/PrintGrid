using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public record QuoteHistoryDto(IReadOnlyList<QuoteDto> Items, int TotalCount, int Page, int PageSize,
    int TotalQuotes, int DraftQuotes, int PendingReviewQuotes, int ReadyQuotes);
public record GetQuoteHistoryQuery(Guid CustomerId, Guid ModelId, QuoteStatus? Status, string? Search,
    DateOnly? From, DateOnly? To, string Sort = "newest", int Page = 1, int PageSize = 5) : IRequest<Result<QuoteHistoryDto>>;

public sealed class GetQuoteHistoryQueryHandler(IQuoteRepository quotes, IDateTimeProvider clock)
    : IRequestHandler<GetQuoteHistoryQuery, Result<QuoteHistoryDto>>
{
    public async Task<Result<QuoteHistoryDto>> Handle(GetQuoteHistoryQuery request, CancellationToken ct)
    {
        if (request.CustomerId == Guid.Empty || request.ModelId == Guid.Empty || request.Page is < 1 or > 1_000_000
            || request.PageSize is < 1 or > 50 || request.From > request.To || request.From == DateOnly.MinValue
            || request.To == DateOnly.MaxValue || request.Search?.Length > 100
            || request.Status.HasValue && !Enum.IsDefined(request.Status.Value)
            || request.Sort is not ("newest" or "oldest" or "price-low" or "price-high"))
            return Result.Failure<QuoteHistoryDto>(Error.Validation("Bộ lọc hoặc phân trang báo giá không hợp lệ."));
        var page = await quotes.GetHistoryAsync(new QuoteHistoryFilter(request.CustomerId, request.ModelId,
            request.Status, request.Search, request.From, request.To, request.Sort, request.Page, request.PageSize, clock.UtcNow), ct);
        var items = page.Items.Select(q =>
        {
            var dto = QuoteMappings.ToCustomerDto(q);
            return q.Status == QuoteStatus.Ready && q.ExpiresAt <= clock.UtcNow ? dto with { Status = "Expired" } : dto;
        }).ToList();
        return Result.Success(new QuoteHistoryDto(items, page.TotalCount, page.Page, page.PageSize,
            page.TotalQuotes, page.DraftQuotes, page.PendingReviewQuotes, page.ReadyQuotes));
    }
}
