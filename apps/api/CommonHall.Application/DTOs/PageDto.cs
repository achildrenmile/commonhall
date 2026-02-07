using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;

namespace CommonHall.Application.DTOs;

public sealed record PageDto
{
    public required Guid Id { get; init; }
    public required Guid SpaceId { get; init; }
    public required string SpaceName { get; init; }
    public required string SpaceSlug { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string Content { get; init; }
    public required ContentStatus Status { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public string? MetaDescription { get; init; }
    public required int SortOrder { get; init; }
    public required bool IsHomePage { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public UserSummaryDto? CreatedBy { get; init; }

    public static PageDto FromEntity(Page page, User? creator = null) => new()
    {
        Id = page.Id,
        SpaceId = page.SpaceId,
        SpaceName = page.Space?.Name ?? string.Empty,
        SpaceSlug = page.Space?.Slug ?? string.Empty,
        Title = page.Title,
        Slug = page.Slug,
        Content = page.Content,
        Status = page.Status,
        PublishedAt = page.PublishedAt,
        MetaDescription = page.MetaDescription,
        SortOrder = page.SortOrder,
        IsHomePage = page.IsHomePage,
        CreatedAt = page.CreatedAt,
        UpdatedAt = page.UpdatedAt,
        CreatedBy = creator != null ? UserSummaryDto.FromEntity(creator) : null
    };
}

public sealed record PageListDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required ContentStatus Status { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public required int SortOrder { get; init; }
    public required bool IsHomePage { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public static PageListDto FromEntity(Page page) => new()
    {
        Id = page.Id,
        Title = page.Title,
        Slug = page.Slug,
        Status = page.Status,
        PublishedAt = page.PublishedAt,
        SortOrder = page.SortOrder,
        IsHomePage = page.IsHomePage,
        UpdatedAt = page.UpdatedAt
    };
}

public sealed record PageDetailDto
{
    public required Guid Id { get; init; }
    public required Guid SpaceId { get; init; }
    public required string SpaceName { get; init; }
    public required string SpaceSlug { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string Content { get; init; }
    public required ContentStatus Status { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public string? MetaDescription { get; init; }
    public string? VisibilityRule { get; init; }
    public required int SortOrder { get; init; }
    public required bool IsHomePage { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public UserSummaryDto? CreatedBy { get; init; }
    public required int VersionCount { get; init; }

    public static PageDetailDto FromEntity(Page page, User? creator, int versionCount) => new()
    {
        Id = page.Id,
        SpaceId = page.SpaceId,
        SpaceName = page.Space?.Name ?? string.Empty,
        SpaceSlug = page.Space?.Slug ?? string.Empty,
        Title = page.Title,
        Slug = page.Slug,
        Content = page.Content,
        Status = page.Status,
        PublishedAt = page.PublishedAt,
        MetaDescription = page.MetaDescription,
        VisibilityRule = page.VisibilityRule,
        SortOrder = page.SortOrder,
        IsHomePage = page.IsHomePage,
        CreatedAt = page.CreatedAt,
        UpdatedAt = page.UpdatedAt,
        CreatedBy = creator != null ? UserSummaryDto.FromEntity(creator) : null,
        VersionCount = versionCount
    };
}

public sealed record PageVersionDto
{
    public required Guid Id { get; init; }
    public required int VersionNumber { get; init; }
    public string? ChangeDescription { get; init; }
    public UserSummaryDto? CreatedBy { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public static PageVersionDto FromEntity(PageVersion version, User? creator) => new()
    {
        Id = version.Id,
        VersionNumber = version.VersionNumber,
        ChangeDescription = version.ChangeDescription,
        CreatedBy = creator != null ? UserSummaryDto.FromEntity(creator) : null,
        CreatedAt = version.CreatedAt
    };
}
