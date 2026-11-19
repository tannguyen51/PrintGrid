using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Domain.Repositories;

public interface IQuoteRepository
{
    Task<Quote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Quote>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Quote>> GetDraftsAsync(CancellationToken cancellationToken = default);
    Task<QuoteHistoryPage> GetHistoryAsync(QuoteHistoryFilter filter, CancellationToken cancellationToken = default);
    Task AddAsync(Quote quote, CancellationToken cancellationToken = default);
}

public sealed record QuoteHistoryFilter(Guid CustomerId, Guid ModelId, Enums.QuoteStatus? Status,
    string? Search, DateOnly? From, DateOnly? To, string Sort, int Page, int PageSize, DateTime NowUtc);
public sealed record QuoteHistoryPage(IReadOnlyList<Quote> Items, int TotalCount, int Page, int PageSize,
    int TotalQuotes, int DraftQuotes, int PendingReviewQuotes, int ReadyQuotes);
