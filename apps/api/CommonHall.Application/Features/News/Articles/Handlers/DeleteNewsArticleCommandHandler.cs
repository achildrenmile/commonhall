using CommonHall.Application.Common;
using CommonHall.Application.Features.News.Articles.Commands;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Articles.Handlers;

public sealed class DeleteNewsArticleCommandHandler : IRequestHandler<DeleteNewsArticleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeleteNewsArticleCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteNewsArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _context.NewsArticles
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (article is null)
        {
            throw new NotFoundException("NewsArticle", request.Id);
        }

        // Soft delete
        article.IsDeleted = true;
        article.DeletedAt = DateTimeOffset.UtcNow;
        article.DeletedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
