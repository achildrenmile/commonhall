using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.Pages.Commands;

public sealed record SetHomePageCommand : IRequest
{
    public Guid SpaceId { get; init; }
    public Guid PageId { get; init; }
}

public sealed class SetHomePageCommandValidator : AbstractValidator<SetHomePageCommand>
{
    public SetHomePageCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty().WithMessage("Space ID is required.");

        RuleFor(x => x.PageId)
            .NotEmpty().WithMessage("Page ID is required.");
    }
}
