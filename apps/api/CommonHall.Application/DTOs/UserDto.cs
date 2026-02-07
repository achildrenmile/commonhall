using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;

namespace CommonHall.Application.DTOs;

public sealed record UserDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Department { get; init; }
    public string? Location { get; init; }
    public string? JobTitle { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Bio { get; init; }
    public string? PreferredLanguage { get; init; }
    public required UserRole Role { get; init; }
    public required bool IsActive { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public static UserDto FromEntity(User user) => new()
    {
        Id = user.Id,
        Email = user.Email!,
        DisplayName = user.DisplayName,
        FirstName = user.FirstName,
        LastName = user.LastName,
        AvatarUrl = user.AvatarUrl,
        Department = user.Department,
        Location = user.Location,
        JobTitle = user.JobTitle,
        PhoneNumber = user.PhoneNumber,
        Bio = user.Bio,
        PreferredLanguage = user.PreferredLanguage,
        Role = user.Role,
        IsActive = user.IsActive,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt
    };
}
