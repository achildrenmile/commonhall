using CommonHall.Application.DTOs;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.Collections.Commands;

public sealed record UpdateCollectionCommand : IRequest<FileCollectionDto>
{
    public required Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
}

public sealed class UpdateCollectionCommandValidator : AbstractValidator<UpdateCollectionCommand>
{
    public UpdateCollectionCommandValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Collection name must not exceed 200 characters.")
            .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description != null);
    }
}
