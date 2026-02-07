using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Comments.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Comments.Handlers;

public sealed class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, CommentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<User> _userManager;

    public AddCommentCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        UserManager<User> userManager)
    {
        _context = context;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AuthenticationException("UNAUTHORIZED", "User is not authenticated.");
        }

        var article = await _context.NewsArticles
            .FirstOrDefaultAsync(a => a.Id == request.NewsArticleId, cancellationToken);

        if (article is null)
        {
            throw new NotFoundException("NewsArticle", request.NewsArticleId);
        }

        if (!article.AllowComments)
        {
            throw new ForbiddenException("Comments are disabled for this article.");
        }

        if (request.ParentCommentId.HasValue)
        {
            var parentExists = await _context.Comments
                .AnyAsync(c => c.Id == request.ParentCommentId.Value &&
                              c.NewsArticleId == request.NewsArticleId &&
                              !c.IsDeleted, cancellationToken);

            if (!parentExists)
            {
                throw new NotFoundException("Comment (Parent)", request.ParentCommentId.Value);
            }
        }

        var comment = new Comment
        {
            NewsArticleId = request.NewsArticleId,
            AuthorId = _currentUser.UserId.Value,
            ParentCommentId = request.ParentCommentId,
            Body = request.Body,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        var author = await _userManager.FindByIdAsync(_currentUser.UserId.Value.ToString());

        return CommentDto.FromEntity(
            comment,
            UserSummaryDto.FromEntity(author!),
            _currentUser.UserId);
    }
}
