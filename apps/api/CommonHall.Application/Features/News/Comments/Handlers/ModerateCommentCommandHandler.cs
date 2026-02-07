using CommonHall.Application.Common;
using CommonHall.Application.Features.News.Comments.Commands;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Comments.Handlers;

public sealed class ModerateCommentCommandHandler : IRequestHandler<ModerateCommentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ModerateCommentCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(ModerateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (comment is null)
        {
            throw new NotFoundException("Comment", request.Id);
        }

        comment.IsModerated = request.IsModerated;
        comment.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
