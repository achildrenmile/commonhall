using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.Spaces.Commands;

public sealed record AddSpaceAdminCommand : IRequest
{
    public Guid SpaceId { get; init; }
    public Guid UserId { get; init; }
}

public sealed class AddSpaceAdminCommandValidator : AbstractValidator<AddSpaceAdminCommand>
{
    public AddSpaceAdminCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty().WithMessage("Space ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
