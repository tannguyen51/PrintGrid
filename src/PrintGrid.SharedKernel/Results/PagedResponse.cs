namespace PrintGrid.SharedKernel.Results;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages)
{
    public static PagedResponse<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize);
        var safePage = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);
        return new PagedResponse<T>(items, totalCount, safePage, safePageSize, totalPages);
    }
}
