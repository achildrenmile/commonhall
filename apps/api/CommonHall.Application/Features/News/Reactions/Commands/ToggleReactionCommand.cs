using CommonHall.Application.DTOs;
using CommonHall.Domain.Enums;
using MediatR;

namespace CommonHall.Application.Features.News.Reactions.Commands;

public sealed record ToggleReactionCommand : IRequest<ToggleReactionResultDto>
{
    public Guid NewsArticleId { get; init; }
    public ReactionType Type { get; init; } = ReactionType.Like;
}
