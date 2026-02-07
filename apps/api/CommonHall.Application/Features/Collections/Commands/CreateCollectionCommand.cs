using CommonHall.Application.DTOs;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.Collections.Commands;

public sealed record CreateCollectionCommand : IRequest<FileCollectionDto>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Guid? SpaceId { get; init; }
}

public sealed class CreateCollectionCommandValidator : AbstractValidator<CreateCollectionCommand>
{
    public CreateCollectionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Collection name is required.")
            .MaximumLength(200).WithMessage("Collection name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
    }
}
