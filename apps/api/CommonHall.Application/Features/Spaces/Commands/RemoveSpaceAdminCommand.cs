using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.Spaces.Commands;

public sealed record RemoveSpaceAdminCommand : IRequest
{
    public Guid SpaceId { get; init; }
    public Guid UserId { get; init; }
}

public sealed class RemoveSpaceAdminCommandValidator : AbstractValidator<RemoveSpaceAdminCommand>
{
    public RemoveSpaceAdminCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty().WithMessage("Space ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
