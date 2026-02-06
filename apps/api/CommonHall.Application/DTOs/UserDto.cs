namespace CommonHall.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl,
    string? Department,
    string? JobTitle,
    bool IsActive,
    DateTime CreatedAt
);
