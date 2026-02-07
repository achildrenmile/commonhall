using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;

namespace CommonHall.Application.DTOs;

public sealed record UserGroupDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required GroupType Type { get; init; }
    public required bool IsSystem { get; init; }
    public required int MemberCount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public static UserGroupDto FromEntity(UserGroup group, int memberCount = 0) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Description = group.Description,
        Type = group.Type,
        IsSystem = group.IsSystem,
        MemberCount = memberCount,
        CreatedAt = group.CreatedAt
    };
}
