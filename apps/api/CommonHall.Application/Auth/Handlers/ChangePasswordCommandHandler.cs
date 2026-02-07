using CommonHall.Application.Auth.Commands;
using CommonHall.Application.Common;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CommonHall.Application.Auth.Handlers;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUser _currentUser;

    public ChangePasswordCommandHandler(UserManager<User> userManager, ICurrentUser currentUser)
    {
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new AuthenticationException("UNAUTHORIZED", "User is not authenticated.");
        }

        var user = await _userManager.FindByIdAsync(_currentUser.UserId.Value.ToString());
        if (user is null)
        {
            throw new NotFoundException("User", _currentUser.UserId.Value);
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new AuthenticationException("PASSWORD_CHANGE_FAILED", errors);
        }
    }
}
