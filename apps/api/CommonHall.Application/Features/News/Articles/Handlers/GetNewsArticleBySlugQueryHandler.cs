using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Articles.Queries;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Articles.Handlers;

public sealed class GetNewsArticleBySlugQueryHandler : IRequestHandler<GetNewsArticleBySlugQuery, NewsArticleDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUser _currentUser;
    private readonly IViewCountService _viewCountService;

    public GetNewsArticleBySlugQueryHandler(
        IApplicationDbContext context,
        UserManager<User> userManager,
        ICurrentUser currentUser,
        IViewCountService viewCountService)
    {
        _context = context;
        _userManager = userManager;
        _currentUser = currentUser;
        _viewCountService = viewCountService;
    }

    public async Task<NewsArticleDetailDto> Handle(GetNewsArticleBySlugQuery request, CancellationToken cancellationToken)
    {
        var article = await _context.NewsArticles
            .Include(a => a.Channel)
            .Include(a => a.Space)
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Slug == request.Slug, cancellationToken);

        if (article is null)
        {
            throw new NotFoundException("NewsArticle", request.Slug);
        }

        // Increment view count if requested
        if (request.IncrementView)
        {
            await _viewCountService.TryIncrementViewCountAsync(article.Id, _currentUser.UserId, cancellationToken);
            // Refresh view count
            article.ViewCount = await _context.NewsArticles
                .Where(a => a.Id == article.Id)
                .Select(a => a.ViewCount)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var author = await _userManager.FindByIdAsync(article.AuthorId.ToString());
        var displayAuthor = article.GhostAuthorId.HasValue
            ? await _userManager.FindByIdAsync(article.GhostAuthorId.Value.ToString())
            : author;

        var commentCount = await _context.Comments
            .CountAsync(c => c.NewsArticleId == article.Id && !c.IsDeleted, cancellationToken);

        var likeCount = await _context.Reactions
            .CountAsync(r => r.NewsArticleId == article.Id && r.Type == ReactionType.Like, cancellationToken);

        var userHasLiked = _currentUser.UserId.HasValue &&
            await _context.Reactions.AnyAsync(r =>
                r.NewsArticleId == article.Id &&
                r.UserId == _currentUser.UserId.Value &&
                r.Type == ReactionType.Like,
                cancellationToken);

        var channelDto = article.Channel != null
            ? NewsChannelDto.FromEntity(article.Channel, 0)
            : null;

        var spaceDto = SpaceDto.FromEntity(article.Space, 0);

        var tags = article.ArticleTags
            .Select(at => TagDto.FromEntity(at.Tag, 0))
            .ToList();

        return new NewsArticleDetailDto
        {
            Id = article.Id,
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
            Channel = channelDto,
            Space = spaceDto,
            Author = UserSummaryDto.FromEntity(author!),
            DisplayAuthor = UserSummaryDto.FromEntity(displayAuthor ?? author!),
            Tags = tags,
            CommentCount = commentCount,
            LikeCount = likeCount,
            ViewCount = article.ViewCount,
            UserHasLiked = userHasLiked,
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt
        };
    }
}
