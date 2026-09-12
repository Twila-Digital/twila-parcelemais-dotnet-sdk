namespace ParceleMais;

public sealed class PagedResult<T>(IReadOnlyList<T> items, bool hasNext, bool hasPrevious, int pageNumber, int pageSize, int totalCount)
{
    public IReadOnlyList<T> Items { get; } = items;
    public bool HasNext { get; } = hasNext;
    public bool HasPrevious { get; } = hasPrevious;
    public int PageNumber { get; } = pageNumber;
    public int PageSize { get; } = pageSize;
    public int TotalCount { get; } = totalCount;
}
