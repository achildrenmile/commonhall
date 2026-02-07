using CommonHall.Domain.Entities;

namespace CommonHall.Application.DTOs;

public sealed record SpaceDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public string? CoverImageUrl { get; init; }
    public Guid? ParentSpaceId { get; init; }
    public required int SortOrder { get; init; }
    public required int PageCount { get; init; }
    public required bool IsDefault { get; init; }

    public static SpaceDto FromEntity(Space space, int pageCount = 0) => new()
    {
        Id = space.Id,
        Name = space.Name,
        Slug = space.Slug,
        Description = space.Description,
        IconUrl = space.IconUrl,
        CoverImageUrl = space.CoverImageUrl,
        ParentSpaceId = space.ParentSpaceId,
        SortOrder = space.SortOrder,
        PageCount = pageCount,
        IsDefault = space.IsDefault
    };
}

public sealed record SpaceDetailDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public string? CoverImageUrl { get; init; }
    public Guid? ParentSpaceId { get; init; }
    public required int SortOrder { get; init; }
    public required int PageCount { get; init; }
    public required bool IsDefault { get; init; }
    public required List<PageListDto> Pages { get; init; }
    public required List<UserSummaryDto> Admins { get; init; }
    public required List<SpaceDto> ChildSpaces { get; init; }

    public static SpaceDetailDto FromEntity(
        Space space,
        List<PageListDto> pages,
        List<UserSummaryDto> admins,
        List<SpaceDto> childSpaces) => new()
    {
        Id = space.Id,
        Name = space.Name,
        Slug = space.Slug,
        Description = space.Description,
        IconUrl = space.IconUrl,
        CoverImageUrl = space.CoverImageUrl,
        ParentSpaceId = space.ParentSpaceId,
        SortOrder = space.SortOrder,
        PageCount = pages.Count,
        IsDefault = space.IsDefault,
        Pages = pages,
        Admins = admins,
        ChildSpaces = childSpaces
    };
}
