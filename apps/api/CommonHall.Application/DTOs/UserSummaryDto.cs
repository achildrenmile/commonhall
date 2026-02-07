using CommonHall.Domain.Entities;

namespace CommonHall.Application.DTOs;

public sealed record UserSummaryDto
{
    public required Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? JobTitle { get; init; }

    public static UserSummaryDto FromEntity(User user) => new()
    {
        Id = user.Id,
        DisplayName = user.DisplayName,
        AvatarUrl = user.AvatarUrl,
        JobTitle = user.JobTitle
    };
}
