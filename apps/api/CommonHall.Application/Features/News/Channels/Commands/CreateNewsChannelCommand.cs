using CommonHall.Application.DTOs;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.News.Channels.Commands;

public sealed record CreateNewsChannelCommand : IRequest<NewsChannelDto>
{
    public Guid SpaceId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Color { get; init; }
}

public sealed class CreateNewsChannelCommandValidator : AbstractValidator<CreateNewsChannelCommand>
{
    public CreateNewsChannelCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty().WithMessage("Space ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Channel name is required.")
            .MinimumLength(2).WithMessage("Channel name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Channel name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Color)
            .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
            .WithMessage("Color must be a valid hex color code (e.g., #FF5733).")
            .When(x => x.Color is not null);
    }
}
