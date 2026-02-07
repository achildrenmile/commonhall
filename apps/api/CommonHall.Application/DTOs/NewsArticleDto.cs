using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;

namespace CommonHall.Application.DTOs;

public sealed record NewsArticleListDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public string? TeaserText { get; init; }
    public string? TeaserImageUrl { get; init; }
    public required ContentStatus Status { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public required bool IsPinned { get; init; }
    public NewsChannelDto? Channel { get; init; }
    public required UserSummaryDto Author { get; init; }
    public required UserSummaryDto DisplayAuthor { get; init; }
    public required List<TagDto> Tags { get; init; }
    public required int CommentCount { get; init; }
    public required int LikeCount { get; init; }
    public required long ViewCount { get; init; }

    public static NewsArticleListDto FromEntity(
        NewsArticle article,
        UserSummaryDto author,
        UserSummaryDto displayAuthor,
        NewsChannelDto? channel,
        List<TagDto> tags,
        int commentCount,
        int likeCount) => new()
    {
        Id = article.Id,
        Title = article.Title,
        Slug = article.Slug,
        TeaserText = article.TeaserText,
        TeaserImageUrl = article.TeaserImageUrl,
        Status = article.Status,
        PublishedAt = article.PublishedAt,
        IsPinned = article.IsPinned,
        Channel = channel,
        Author = author,
        DisplayAuthor = displayAuthor,
        Tags = tags,
        CommentCount = commentCount,
        LikeCount = likeCount,
        ViewCount = article.ViewCount
    };
}

public sealed record NewsArticleDetailDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public string? TeaserText { get; init; }
    public string? TeaserImageUrl { get; init; }
    public required string Content { get; init; }
    public required ContentStatus Status { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public DateTimeOffset? ScheduledAt { get; init; }
    public required bool IsPinned { get; init; }
    public required bool AllowComments { get; init; }
    public NewsChannelDto? Channel { get; init; }
    public required SpaceDto Space { get; init; }
    public required UserSummaryDto Author { get; init; }
    public required UserSummaryDto DisplayAuthor { get; init; }
    public required List<TagDto> Tags { get; init; }
    public required int CommentCount { get; init; }
    public required int LikeCount { get; init; }
    public required long ViewCount { get; init; }
    public required bool UserHasLiked { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record NewsArticleDto
{
    public required Guid Id { get; init; }
    public required Guid SpaceId { get; init; }
    public Guid? ChannelId { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public string? TeaserText { get; init; }
    public string? TeaserImageUrl { get; init; }
    public required string Content { get; init; }
    public required ContentStatus Status { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public DateTimeOffset? ScheduledAt { get; init; }
    public required bool IsPinned { get; init; }
    public required bool AllowComments { get; init; }
    public required UserSummaryDto Author { get; init; }
    public required UserSummaryDto DisplayAuthor { get; init; }
    public required List<string> Tags { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
