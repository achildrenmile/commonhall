using CommonHall.Application.DTOs;
using CommonHall.Domain.Enums;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.News.Articles.Commands;

public sealed record CreateNewsArticleCommand : IRequest<NewsArticleDto>
{
    public Guid SpaceId { get; init; }
    public Guid? ChannelId { get; init; }
    public required string Title { get; init; }
    public string? TeaserText { get; init; }
    public string? TeaserImageUrl { get; init; }
    public required string Content { get; init; }
    public List<string> Tags { get; init; } = [];
    public ContentStatus Status { get; init; } = ContentStatus.Draft;
    public DateTimeOffset? ScheduledAt { get; init; }
    public Guid? GhostAuthorId { get; init; }
    public bool AllowComments { get; init; } = true;
    public bool IsPinned { get; init; } = false;
    public string? VisibilityRule { get; init; }
}

public sealed class CreateNewsArticleCommandValidator : AbstractValidator<CreateNewsArticleCommand>
{
    public CreateNewsArticleCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty().WithMessage("Space ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(2).WithMessage("Title must be at least 2 characters.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");

        RuleFor(x => x.TeaserText)
            .MaximumLength(500).WithMessage("Teaser text must not exceed 500 characters.")
            .When(x => x.TeaserText is not null);

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .Must(BeValidJson).WithMessage("Content must be valid JSON.");

        RuleFor(x => x.ScheduledAt)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Scheduled date must be in the future.")
            .When(x => x.ScheduledAt.HasValue);

        RuleFor(x => x.Status)
            .Must(s => s != ContentStatus.Scheduled || true)
            .WithMessage("Use ScheduledAt to schedule articles.");
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
