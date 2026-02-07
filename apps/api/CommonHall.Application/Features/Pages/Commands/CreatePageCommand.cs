using CommonHall.Application.DTOs;
using CommonHall.Domain.Enums;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.Pages.Commands;

public sealed record CreatePageCommand : IRequest<PageDto>
{
    public Guid SpaceId { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public ContentStatus Status { get; init; } = ContentStatus.Draft;
    public string? VisibilityRule { get; init; }
}

public sealed class CreatePageCommandValidator : AbstractValidator<CreatePageCommand>
{
    public CreatePageCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty().WithMessage("Space ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Page title is required.")
            .MinimumLength(2).WithMessage("Page title must be at least 2 characters.")
            .MaximumLength(500).WithMessage("Page title must not exceed 500 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .Must(BeValidJson).WithMessage("Content must be valid JSON.");
    }

    private static bool BeValidJson(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        try
        {
            System.Text.Json.JsonDocument.Parse(content);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
