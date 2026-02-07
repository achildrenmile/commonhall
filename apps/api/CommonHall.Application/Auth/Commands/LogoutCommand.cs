using MediatR;

namespace CommonHall.Application.Auth.Commands;

public sealed record LogoutCommand : IRequest
{
    public required string RefreshToken { get; init; }
}
