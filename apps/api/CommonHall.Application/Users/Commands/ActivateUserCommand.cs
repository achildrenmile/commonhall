using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CommonHall.Application.Users.Commands;

public sealed record ActivateUserCommand(Guid Id) : IRequest<UserDto>;

public sealed class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand, UserDto>
{
    private readonly UserManager<User> _userManager;

    public ActivateUserCommandHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserDto> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user is null)
        {
            throw new NotFoundException("User", request.Id);
        }

        user.IsActive = true;

        await _userManager.UpdateAsync(user);

        return UserDto.FromEntity(user);
    }
}
