using CommonHall.Application.Common;
using CommonHall.Application.Features.News.Comments.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Comments.Handlers;

public sealed class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeleteCommentCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (comment is null)
        {
            throw new NotFoundException("Comment", request.Id);
        }

        // Only author or admin can delete
        var isAuthor = _currentUser.UserId.HasValue && comment.AuthorId == _currentUser.UserId.Value;
        var isAdmin = _currentUser.Role == UserRole.Admin;

        if (!isAuthor && !isAdmin)
        {
            throw new ForbiddenException("You can only delete your own comments.");
        }

        // Soft delete
        comment.IsDeleted = true;
        comment.DeletedAt = DateTimeOffset.UtcNow;
        comment.DeletedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
