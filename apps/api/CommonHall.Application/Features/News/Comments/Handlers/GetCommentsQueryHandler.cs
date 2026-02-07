using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Comments.Queries;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Comments.Handlers;

public sealed class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, CursorPaginatedResult<CommentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUser _currentUser;

    public GetCommentsQueryHandler(
        IApplicationDbContext context,
        UserManager<User> userManager,
        ICurrentUser currentUser)
    {
        _context = context;
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task<CursorPaginatedResult<CommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        var articleExists = await _context.NewsArticles
            .AnyAsync(a => a.Id == request.NewsArticleId, cancellationToken);

        if (!articleExists)
        {
            throw new NotFoundException("NewsArticle", request.NewsArticleId);
        }

        // Get top-level comments (no parent)
        var query = _context.Comments
            .Where(c => c.NewsArticleId == request.NewsArticleId && c.ParentCommentId == null && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt);

        // Apply cursor
        if (!string.IsNullOrWhiteSpace(request.Cursor) && Guid.TryParse(request.Cursor, out var cursorId))
        {
            var cursorComment = await _context.Comments
                .Where(c => c.Id == cursorId)
                .Select(c => c.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (cursorComment != default)
            {
                query = (IOrderedQueryable<Comment>)query.Where(c => c.CreatedAt < cursorComment);
            }
        }

        var comments = await query
            .Take(request.Size + 1)
            .ToListAsync(cancellationToken);

        var dtos = new List<CommentDto>();
        foreach (var comment in comments.Take(request.Size))
        {
            var author = await _userManager.FindByIdAsync(comment.AuthorId.ToString());

            // Get replies for this comment
            var replies = await _context.Comments
                .Where(c => c.ParentCommentId == comment.Id && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .Take(5) // Limit initial replies
                .ToListAsync(cancellationToken);

            var replyDtos = new List<CommentDto>();
            foreach (var reply in replies)
            {
                var replyAuthor = await _userManager.FindByIdAsync(reply.AuthorId.ToString());
                replyDtos.Add(CommentDto.FromEntity(
                    reply,
                    UserSummaryDto.FromEntity(replyAuthor!),
                    _currentUser.UserId));
            }

            dtos.Add(CommentDto.FromEntity(
                comment,
                UserSummaryDto.FromEntity(author!),
                _currentUser.UserId,
                replyDtos.Count > 0 ? replyDtos : null));
        }

        return CursorPaginatedResult<CommentDto>.Create(
            dtos,
            request.Size,
            dto => dto.Id.ToString());
    }
}
