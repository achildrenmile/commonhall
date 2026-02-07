using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Articles.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Articles.Handlers;

public sealed class PublishNewsArticleCommandHandler : IRequestHandler<PublishNewsArticleCommand, NewsArticleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<User> _userManager;

    public PublishNewsArticleCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        UserManager<User> userManager)
    {
        _context = context;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<NewsArticleDto> Handle(PublishNewsArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _context.NewsArticles
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (article is null)
        {
            throw new NotFoundException("NewsArticle", request.Id);
        }

        article.Status = ContentStatus.Published;
        article.PublishedAt = DateTimeOffset.UtcNow;
        article.ScheduledAt = null;
        article.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        var author = await _userManager.FindByIdAsync(article.AuthorId.ToString());
        var displayAuthor = article.GhostAuthorId.HasValue
            ? await _userManager.FindByIdAsync(article.GhostAuthorId.Value.ToString())
            : author;

        var tags = await _context.ArticleTags
            .Where(at => at.NewsArticleId == article.Id)
            .Include(at => at.Tag)
            .Select(at => at.Tag.Name)
            .ToListAsync(cancellationToken);

        return new NewsArticleDto
        {
            Id = article.Id,
            SpaceId = article.SpaceId,
            ChannelId = article.ChannelId,
            Title = article.Title,
            Slug = article.Slug,
            TeaserText = article.TeaserText,
            TeaserImageUrl = article.TeaserImageUrl,
            Content = article.Content,
            Status = article.Status,
            PublishedAt = article.PublishedAt,
            ScheduledAt = article.ScheduledAt,
            IsPinned = article.IsPinned,
            AllowComments = article.AllowComments,
            Author = UserSummaryDto.FromEntity(author!),
            DisplayAuthor = UserSummaryDto.FromEntity(displayAuthor ?? author!),
            Tags = tags,
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt
        };
    }
}
