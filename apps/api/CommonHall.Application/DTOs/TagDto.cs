using CommonHall.Domain.Entities;

namespace CommonHall.Application.DTOs;

public sealed record TagDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required int ArticleCount { get; init; }

    public static TagDto FromEntity(Tag tag, int articleCount = 0) => new()
    {
        Id = tag.Id,
        Name = tag.Name,
        Slug = tag.Slug,
        ArticleCount = articleCount
    };
}
