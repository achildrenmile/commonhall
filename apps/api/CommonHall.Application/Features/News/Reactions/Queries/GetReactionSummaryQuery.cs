using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.News.Reactions.Queries;

public sealed record GetReactionSummaryQuery : IRequest<ReactionSummaryDto>
{
    public Guid NewsArticleId { get; init; }
}
