using CommonHall.Application.DTOs;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.Pages.Commands;

public sealed record UpdatePageCommand : IRequest<PageDto>
{
    public Guid Id { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public string? MetaDescription { get; init; }
    public string? VisibilityRule { get; init; }
}

public sealed class UpdatePageCommandValidator : AbstractValidator<UpdatePageCommand>
{
    public UpdatePageCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Page ID is required.");

        RuleFor(x => x.Title)
            .MinimumLength(2).WithMessage("Page title must be at least 2 characters.")
            .MaximumLength(500).WithMessage("Page title must not exceed 500 characters.")
            .When(x => x.Title is not null);

        RuleFor(x => x.Content)
            .Must(BeValidJson!).WithMessage("Content must be valid JSON.")
            .When(x => x.Content is not null);

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("Meta description must not exceed 500 characters.")
            .When(x => x.MetaDescription is not null);
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
