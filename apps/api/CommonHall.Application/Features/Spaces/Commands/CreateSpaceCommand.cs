using CommonHall.Application.DTOs;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.Spaces.Commands;

public sealed record CreateSpaceCommand : IRequest<SpaceDto>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public Guid? ParentSpaceId { get; init; }
}

public sealed class CreateSpaceCommandValidator : AbstractValidator<CreateSpaceCommand>
{
    public CreateSpaceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Space name is required.")
            .MinimumLength(2).WithMessage("Space name must be at least 2 characters.")
            .MaximumLength(500).WithMessage("Space name must not exceed 500 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.IconUrl)
            .MaximumLength(2048).WithMessage("Icon URL must not exceed 2048 characters.")
            .When(x => x.IconUrl is not null);
    }
}
