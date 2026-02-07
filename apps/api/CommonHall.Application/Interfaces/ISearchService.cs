namespace CommonHall.Application.Interfaces;

public interface ISearchService
{
    Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);
    Task<List<SearchSuggestion>> SuggestAsync(string query, int limit = 5, CancellationToken cancellationToken = default);
    Task IndexNewsArticleAsync(Guid articleId, CancellationToken cancellationToken = default);
    Task IndexPageAsync(Guid pageId, CancellationToken cancellationToken = default);
    Task IndexUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task IndexFileAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task DeleteFromIndexAsync(string indexType, Guid id, CancellationToken cancellationToken = default);
    Task ReindexAllAsync(IProgress<ReindexProgress>? progress = null, CancellationToken cancellationToken = default);
    Task EnsureIndexesExistAsync(CancellationToken cancellationToken = default);
}

public record SearchQuery
{
    public string Query { get; init; } = string.Empty;
    public string? Type { get; init; }
    public string? SpaceSlug { get; init; }
    public int From { get; init; } = 0;
    public int Size { get; init; } = 20;
    public Guid? CurrentUserId { get; init; }
}

public record SearchResult
{
    public List<SearchHit> Hits { get; init; } = new();
    public int Total { get; init; }
    public Dictionary<string, int> TypeFacets { get; init; } = new();
    public Dictionary<string, int> SpaceFacets { get; init; } = new();
}

public record SearchHit
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public string? Excerpt { get; init; }
    public string? HighlightedTitle { get; init; }
    public string? HighlightedExcerpt { get; init; }
    public string? Url { get; init; }
    public string? ImageUrl { get; init; }
    public string? Subtitle { get; init; }
    public DateTimeOffset? Date { get; init; }
    public double Score { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record SearchSuggestion
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }
    public string? ImageUrl { get; init; }
    public string? Url { get; init; }
}

public record ReindexProgress
{
    public string CurrentIndex { get; init; } = string.Empty;
    public int ProcessedCount { get; init; }
    public int TotalCount { get; init; }
    public string? Message { get; init; }
}
