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

public sealed record PaginationRequest
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
    public string? Search { get; init; }

    public int Skip => (Page - 1) * Size;
}
