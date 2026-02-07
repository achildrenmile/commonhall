namespace CommonHall.Infrastructure.Search;

public record NewsSearchDocument
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? TeaserText { get; init; }
    public string? BodyText { get; init; }
    public List<string> Tags { get; init; } = new();
    public string? ChannelName { get; init; }
    public string? ChannelSlug { get; init; }
    public string? SpaceName { get; init; }
    public string? SpaceSlug { get; init; }
    public string? AuthorName { get; init; }
    public string? AuthorId { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public required string Slug { get; init; }
    public long ViewCount { get; init; }
    public object? VisibilityRule { get; init; }
    public string? TeaserImageUrl { get; init; }
}

public record PageSearchDocument
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? BodyText { get; init; }
    public string? SpaceName { get; init; }
    public string? SpaceSlug { get; init; }
    public required string Slug { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public record UserSearchDocument
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Department { get; init; }
    public string? Location { get; init; }
    public string? JobTitle { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public bool IsActive { get; init; }
}

public record FileSearchDocument
{
    public required string Id { get; init; }
    public required string OriginalName { get; init; }
    public required string MimeType { get; init; }
    public string? CollectionName { get; init; }
    public string? CollectionId { get; init; }
    public string? AltText { get; init; }
    public long FileSize { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public required string Url { get; init; }
}
