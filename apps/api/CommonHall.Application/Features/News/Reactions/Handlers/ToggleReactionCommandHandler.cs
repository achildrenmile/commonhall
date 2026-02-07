using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Reactions.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Reactions.Handlers;

public sealed class ToggleReactionCommandHandler : IRequestHandler<ToggleReactionCommand, ToggleReactionResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ToggleReactionCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ToggleReactionResultDto> Handle(ToggleReactionCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AuthenticationException("UNAUTHORIZED", "User is not authenticated.");
        }

        var articleExists = await _context.NewsArticles
            .AnyAsync(a => a.Id == request.NewsArticleId, cancellationToken);

        if (!articleExists)
        {
            throw new NotFoundException("NewsArticle", request.NewsArticleId);
        }

        var existingReaction = await _context.Reactions
            .FirstOrDefaultAsync(r =>
                r.NewsArticleId == request.NewsArticleId &&
                r.UserId == _currentUser.UserId.Value &&
                r.Type == request.Type, cancellationToken);

        bool isReacted;

        if (existingReaction is not null)
        {
            _context.Reactions.Remove(existingReaction);
            isReacted = false;
        }
        else
        {
            var reaction = new Reaction
            {
                NewsArticleId = request.NewsArticleId,
                UserId = _currentUser.UserId.Value,
                Type = request.Type,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _context.Reactions.Add(reaction);
            isReacted = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var totalCount = await _context.Reactions
            .CountAsync(r => r.NewsArticleId == request.NewsArticleId && r.Type == request.Type, cancellationToken);

        return new ToggleReactionResultDto
        {
            IsReacted = isReacted,
            TotalCount = totalCount
        };
    }
}
