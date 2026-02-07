using CommonHall.Application.Auth.Commands;
using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using MediatR;

namespace CommonHall.Application.Auth.Handlers;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
    }
}
