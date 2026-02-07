using CommonHall.Domain.Entities;

namespace CommonHall.Application.DTOs;

public sealed record NewsChannelDto
{
    public required Guid Id { get; init; }
    public required Guid SpaceId { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public string? Color { get; init; }
    public required int SortOrder { get; init; }
    public required int ArticleCount { get; init; }

    public static NewsChannelDto FromEntity(NewsChannel channel, int articleCount = 0) => new()
    {
        Id = channel.Id,
        SpaceId = channel.SpaceId,
        Name = channel.Name,
        Slug = channel.Slug,
        Description = channel.Description,
        Color = channel.Color,
        SortOrder = channel.SortOrder,
        ArticleCount = articleCount
    };
}
