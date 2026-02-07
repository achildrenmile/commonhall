using CommonHall.Application.DTOs;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.News.Articles.Commands;

public sealed record UpdateNewsArticleCommand : IRequest<NewsArticleDto>
{
    public Guid Id { get; init; }
    public Guid? ChannelId { get; init; }
    public string? Title { get; init; }
    public string? TeaserText { get; init; }
    public string? TeaserImageUrl { get; init; }
    public string? Content { get; init; }
    public List<string>? Tags { get; init; }
    public Guid? GhostAuthorId { get; init; }
    public bool? AllowComments { get; init; }
    public bool? IsPinned { get; init; }
    public string? VisibilityRule { get; init; }
}

public sealed class UpdateNewsArticleCommandValidator : AbstractValidator<UpdateNewsArticleCommand>
{
    public UpdateNewsArticleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Article ID is required.");

        RuleFor(x => x.Title)
            .MinimumLength(2).WithMessage("Title must be at least 2 characters.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.")
            .When(x => x.Title is not null);

        RuleFor(x => x.TeaserText)
            .MaximumLength(500).WithMessage("Teaser text must not exceed 500 characters.")
            .When(x => x.TeaserText is not null);

        RuleFor(x => x.Content)
            .Must(BeValidJson!).WithMessage("Content must be valid JSON.")
            .When(x => x.Content is not null);
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
