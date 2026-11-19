using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

public class QuoteRepository(PrintGridDbContext context) : IQuoteRepository
{
    public Task<Quote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Set<Quote>()
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Quote>> GetByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        await context.Set<Quote>()
            .Include(q => q.Items)
            .Where(q => q.CustomerId == customerId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Quote>> GetDraftsAsync(CancellationToken cancellationToken = default) =>
        await context.Set<Quote>()
            .Include(q => q.Items)
            .Where(q => q.Status == Domain.Enums.QuoteStatus.PendingReview)
            .OrderBy(q => q.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Quote quote, CancellationToken cancellationToken = default) =>
        await context.Set<Quote>().AddAsync(quote, cancellationToken);

    public async Task<QuoteHistoryPage> GetHistoryAsync(QuoteHistoryFilter filter, CancellationToken ct = default)
    {
        var all = context.Set<Quote>().AsNoTracking()
            .Where(q => q.CustomerId == filter.CustomerId && q.Items.Any(i => i.ModelId == filter.ModelId));
        var counts = await all.GroupBy(_ => 1).Select(g => new
        {
            Total = g.Count(), Draft = g.Count(q => q.Status == Domain.Enums.QuoteStatus.Draft),
            Pending = g.Count(q => q.Status == Domain.Enums.QuoteStatus.PendingReview),
            Ready = g.Count(q => q.Status == Domain.Enums.QuoteStatus.Ready && (q.ExpiresAt == null || q.ExpiresAt > filter.NowUtc))
        }).SingleOrDefaultAsync(ct);
        var query = all;
        if (filter.Status == Domain.Enums.QuoteStatus.Expired)
            query = query.Where(q => q.Status == Domain.Enums.QuoteStatus.Expired || q.Status == Domain.Enums.QuoteStatus.Ready && q.ExpiresAt <= filter.NowUtc);
        else if (filter.Status == Domain.Enums.QuoteStatus.Ready)
            query = query.Where(q => q.Status == Domain.Enums.QuoteStatus.Ready && (q.ExpiresAt == null || q.ExpiresAt > filter.NowUtc));
        else if (filter.Status is { } status)
            query = query.Where(q => q.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(q => q.Id.ToString().ToLower().Contains(term) || q.Items.Any(i =>
                i.Configuration.MaterialCode.ToLower().Contains(term) || i.Configuration.ColorCode.ToLower().Contains(term)));
        }
        if (filter.From is { } from)
        {
            var start = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
            query = query.Where(q => q.CreatedAt >= start);
        }
        if (filter.To is { } to)
        {
            var end = DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
            query = query.Where(q => q.CreatedAt < end);
        }
        var total = await query.CountAsync(ct);
        var ordered = filter.Sort switch
        {
            "oldest" => query.OrderBy(q => q.CreatedAt),
            "price-low" => query.OrderBy(q => q.TotalPrice.Amount),
            "price-high" => query.OrderByDescending(q => q.TotalPrice.Amount),
            _ => query.OrderByDescending(q => q.CreatedAt)
        };
        var items = await ordered.ThenBy(q => q.Id).Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Include(q => q.Items).ToListAsync(ct);
        return new QuoteHistoryPage(items, total, filter.Page, filter.PageSize, counts?.Total ?? 0,
            counts?.Draft ?? 0, counts?.Pending ?? 0, counts?.Ready ?? 0);
    }
}
