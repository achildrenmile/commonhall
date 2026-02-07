using CommonHall.Application.DTOs;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.News.Articles.Commands;

public sealed record ScheduleNewsArticleCommand : IRequest<NewsArticleDto>
{
    public Guid Id { get; init; }
    public DateTimeOffset ScheduledAt { get; init; }
}

public sealed class ScheduleNewsArticleCommandValidator : AbstractValidator<ScheduleNewsArticleCommand>
{
    public ScheduleNewsArticleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Article ID is required.");

        RuleFor(x => x.ScheduledAt)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Scheduled date must be in the future.");
    }
}
