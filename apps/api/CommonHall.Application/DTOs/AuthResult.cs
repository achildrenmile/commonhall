namespace CommonHall.Application.DTOs;

public sealed record AuthResult
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required UserDto User { get; init; }
}
