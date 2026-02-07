using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CommonHall.Application.Users.Commands;

public sealed record UpdateUserCommand : IRequest<UserDto>
{
    public Guid Id { get; init; }
    public string? DisplayName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Department { get; init; }
    public string? Location { get; init; }
    public string? JobTitle { get; init; }
    public UserRole? Role { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.DisplayName)
            .MinimumLength(2).MaximumLength(200)
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.FirstName)
            .MaximumLength(100)
            .When(x => x.FirstName is not null);

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .When(x => x.LastName is not null);
    }
}

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly UserManager<User> _userManager;

    public UpdateUserCommandHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user is null)
        {
            throw new NotFoundException("User", request.Id);
        }

        if (request.DisplayName is not null) user.DisplayName = request.DisplayName;
        if (request.FirstName is not null) user.FirstName = request.FirstName;
        if (request.LastName is not null) user.LastName = request.LastName;
        if (request.Department is not null) user.Department = request.Department;
        if (request.Location is not null) user.Location = request.Location;
        if (request.JobTitle is not null) user.JobTitle = request.JobTitle;
        if (request.Role.HasValue) user.Role = request.Role.Value;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

        await _userManager.UpdateAsync(user);

        return UserDto.FromEntity(user);
    }
}
