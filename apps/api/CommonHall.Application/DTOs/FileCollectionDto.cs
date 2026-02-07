using CommonHall.Domain.Entities;

namespace CommonHall.Application.DTOs;

public sealed record FileCollectionDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Guid? SpaceId { get; init; }
    public string? SpaceName { get; init; }
    public required int FileCount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public static FileCollectionDto FromEntity(FileCollection collection, int fileCount)
    {
        return new FileCollectionDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            SpaceId = collection.SpaceId,
            SpaceName = collection.Space?.Name,
            FileCount = fileCount,
            CreatedAt = collection.CreatedAt
        };
    }
}
