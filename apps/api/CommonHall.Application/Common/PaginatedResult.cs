namespace CommonHall.Application.Common;

public sealed record PaginatedResult<T>
{
    public required List<T> Items { get; init; }
    public required int Total { get; init; }
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }

    public static PaginatedResult<T> Create(List<T> items, int total, int pageSize, string? nextCursor = null) =>
        new()
        {
            Items = items,
            Total = total,
            NextCursor = nextCursor,
            HasMore = items.Count == pageSize
        };
}

public sealed record CursorPaginatedResult<T>
{
    public required List<T> Items { get; init; }
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }

    public static CursorPaginatedResult<T> Create(List<T> items, int requestedSize, Func<T, string>? cursorSelector = null)
    {
        var hasMore = items.Count > requestedSize;
        var resultItems = hasMore ? items.Take(requestedSize).ToList() : items;
        var nextCursor = hasMore && cursorSelector != null && resultItems.Count > 0
            ? cursorSelector(resultItems.Last())
            : null;

        return new CursorPaginatedResult<T>
        {
            Items = resultItems,
            NextCursor = nextCursor,
            HasMore = hasMore
        };
    }
}

public sealed record PaginationRequest
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
    public string? Search { get; init; }

    public int Skip => (Page - 1) * Size;
}
