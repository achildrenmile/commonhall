using CommonHall.Application.DTOs;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.Spaces.Commands;

public sealed record UpdateSpaceCommand : IRequest<SpaceDto>
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public string? CoverImageUrl { get; init; }
    public int? SortOrder { get; init; }
}

public sealed class UpdateSpaceCommandValidator : AbstractValidator<UpdateSpaceCommand>
{
    public UpdateSpaceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Space ID is required.");

        RuleFor(x => x.Name)
            .MinimumLength(2).WithMessage("Space name must be at least 2 characters.")
            .MaximumLength(500).WithMessage("Space name must not exceed 500 characters.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.IconUrl)
            .MaximumLength(2048).WithMessage("Icon URL must not exceed 2048 characters.")
            .When(x => x.IconUrl is not null);

        RuleFor(x => x.CoverImageUrl)
            .MaximumLength(2048).WithMessage("Cover image URL must not exceed 2048 characters.")
            .When(x => x.CoverImageUrl is not null);
    }
}
