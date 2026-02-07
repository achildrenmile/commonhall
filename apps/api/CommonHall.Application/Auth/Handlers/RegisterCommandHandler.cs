using CommonHall.Application.Auth.Commands;
using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using MediatR;

namespace CommonHall.Application.Auth.Handlers;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return await _authService.RegisterAsync(request.Email, request.Password, request.DisplayName, cancellationToken);
    }
}
