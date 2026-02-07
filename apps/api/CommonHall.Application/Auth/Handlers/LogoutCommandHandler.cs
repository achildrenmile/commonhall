using CommonHall.Application.Auth.Commands;
using CommonHall.Application.Interfaces;
using MediatR;

namespace CommonHall.Application.Auth.Handlers;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IAuthService _authService;

    public LogoutCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken, cancellationToken);
    }
}
