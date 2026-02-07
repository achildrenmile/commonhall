using CommonHall.Application.Common;
using CommonHall.Application.Features.News.Articles.Commands;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Articles.Handlers;

public sealed class PinNewsArticleCommandHandler : IRequestHandler<PinNewsArticleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public PinNewsArticleCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(PinNewsArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _context.NewsArticles
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (article is null)
        {
            throw new NotFoundException("NewsArticle", request.Id);
        }

        article.IsPinned = request.IsPinned;
        article.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
