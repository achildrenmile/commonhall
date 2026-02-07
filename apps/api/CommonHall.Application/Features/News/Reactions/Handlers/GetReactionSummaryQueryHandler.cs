using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Reactions.Queries;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Reactions.Handlers;

public sealed class GetReactionSummaryQueryHandler : IRequestHandler<GetReactionSummaryQuery, ReactionSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetReactionSummaryQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ReactionSummaryDto> Handle(GetReactionSummaryQuery request, CancellationToken cancellationToken)
    {
        var articleExists = await _context.NewsArticles
            .AnyAsync(a => a.Id == request.NewsArticleId, cancellationToken);

        if (!articleExists)
        {
            throw new NotFoundException("NewsArticle", request.NewsArticleId);
        }

        var totalLikes = await _context.Reactions
            .CountAsync(r => r.NewsArticleId == request.NewsArticleId && r.Type == ReactionType.Like, cancellationToken);

        var userHasLiked = false;
        if (_currentUser.UserId.HasValue)
        {
            userHasLiked = await _context.Reactions
                .AnyAsync(r =>
                    r.NewsArticleId == request.NewsArticleId &&
                    r.UserId == _currentUser.UserId.Value &&
                    r.Type == ReactionType.Like, cancellationToken);
        }

        return new ReactionSummaryDto
        {
            TotalLikes = totalLikes,
            UserHasLiked = userHasLiked
        };
    }
}
