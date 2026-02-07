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

public sealed class GetNewsFeedQueryHandler : IRequestHandler<GetNewsFeedQuery, CursorPaginatedResult<NewsArticleListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUser _currentUser;
    private readonly ITargetingService _targetingService;

    public GetNewsFeedQueryHandler(
        IApplicationDbContext context,
        UserManager<User> userManager,
        ICurrentUser currentUser,
        ITargetingService targetingService)
    {
        _context = context;
        _userManager = userManager;
        _currentUser = currentUser;
        _targetingService = targetingService;
    }

    public async Task<CursorPaginatedResult<NewsArticleListDto>> Handle(GetNewsFeedQuery request, CancellationToken cancellationToken)
    {
        var query = _context.NewsArticles
            .Include(a => a.Channel)
            .Include(a => a.Space)
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SpaceSlug))
        {
            query = query.Where(a => a.Space.Slug == request.SpaceSlug);
        }

        if (!string.IsNullOrWhiteSpace(request.ChannelSlug))
        {
            query = query.Where(a => a.Channel != null && a.Channel.Slug == request.ChannelSlug);
        }

        if (!string.IsNullOrWhiteSpace(request.TagSlug))
        {
            query = query.Where(a => a.ArticleTags.Any(at => at.Tag.Slug == request.TagSlug));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }
        else
        {
            // Default to published only for public feed
            query = query.Where(a => a.Status == ContentStatus.Published);
        }

        if (request.IsPinned.HasValue)
        {
            query = query.Where(a => a.IsPinned == request.IsPinned.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(searchLower) ||
                (a.TeaserText != null && a.TeaserText.ToLower().Contains(searchLower)));
        }

        // Apply cursor pagination
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            if (Guid.TryParse(request.Cursor, out var cursorId))
            {
                var cursorArticle = await _context.NewsArticles
                    .Where(a => a.Id == cursorId)
                    .Select(a => new { a.PublishedAt, a.IsPinned })
                    .FirstOrDefaultAsync(cancellationToken);

                if (cursorArticle is not null)
                {
                    // Order by pinned first, then by published date
                    query = query.Where(a =>
                        (a.IsPinned == cursorArticle.IsPinned && a.PublishedAt < cursorArticle.PublishedAt) ||
                        (!a.IsPinned && cursorArticle.IsPinned));
                }
            }
        }

        // Order by pinned first, then by published date descending
        var orderedQuery = query
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishedAt);

        // Fetch extra to account for targeting filtering
        var fetchSize = request.Size * 2 + 1; // Fetch extra to ensure we have enough after filtering
        var articles = await orderedQuery
            .Take(fetchSize)
            .ToListAsync(cancellationToken);

        // Apply visibility filtering based on current user
        var userId = _currentUser.UserId;
        var visibleArticles = userId.HasValue
            ? (await _targetingService.FilterVisibleAsync(
                userId.Value,
                articles,
                a => a.VisibilityRule,
                cancellationToken)).ToList()
            : articles.Where(a => string.IsNullOrEmpty(a.VisibilityRule)).ToList();

        var dtos = new List<NewsArticleListDto>();
        foreach (var article in visibleArticles.Take(request.Size))
        {
            var author = await _userManager.FindByIdAsync(article.AuthorId.ToString());
            var displayAuthor = article.GhostAuthorId.HasValue
                ? await _userManager.FindByIdAsync(article.GhostAuthorId.Value.ToString())
                : author;

            var commentCount = await _context.Comments
                .CountAsync(c => c.NewsArticleId == article.Id && !c.IsDeleted, cancellationToken);

            var likeCount = await _context.Reactions
                .CountAsync(r => r.NewsArticleId == article.Id && r.Type == ReactionType.Like, cancellationToken);

            var channelDto = article.Channel != null
                ? NewsChannelDto.FromEntity(article.Channel, 0)
                : null;

            var tags = article.ArticleTags
                .Select(at => TagDto.FromEntity(at.Tag, 0))
                .ToList();

            dtos.Add(NewsArticleListDto.FromEntity(
                article,
                UserSummaryDto.FromEntity(author!),
                UserSummaryDto.FromEntity(displayAuthor ?? author!),
                channelDto,
                tags,
                commentCount,
                likeCount));
        }

        return CursorPaginatedResult<NewsArticleListDto>.Create(
            dtos,
            request.Size,
            dto => dto.Id.ToString());
    }
}
