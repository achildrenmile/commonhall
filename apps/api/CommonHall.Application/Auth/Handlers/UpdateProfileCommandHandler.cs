using CommonHall.Application.Auth.Commands;
using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CommonHall.Application.Auth.Handlers;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserDto>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUser _currentUser;

    public UpdateProfileCommandHandler(UserManager<User> userManager, ICurrentUser currentUser)
    {
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task<UserDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
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

        // Update only provided fields
        if (request.DisplayName is not null) user.DisplayName = request.DisplayName;
        if (request.FirstName is not null) user.FirstName = request.FirstName;
        if (request.LastName is not null) user.LastName = request.LastName;
        if (request.AvatarUrl is not null) user.AvatarUrl = request.AvatarUrl;
        if (request.Department is not null) user.Department = request.Department;
        if (request.Location is not null) user.Location = request.Location;
        if (request.JobTitle is not null) user.JobTitle = request.JobTitle;
        if (request.PhoneNumber is not null) user.PhoneNumber = request.PhoneNumber;
        if (request.Bio is not null) user.Bio = request.Bio;
        if (request.PreferredLanguage is not null) user.PreferredLanguage = request.PreferredLanguage;

        await _userManager.UpdateAsync(user);

        return UserDto.FromEntity(user);
    }
}
