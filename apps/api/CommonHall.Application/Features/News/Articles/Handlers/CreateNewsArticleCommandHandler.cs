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

public sealed class CreateNewsArticleCommandHandler : IRequestHandler<CreateNewsArticleCommand, NewsArticleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISlugService _slugService;
    private readonly ITagService _tagService;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<User> _userManager;

    public CreateNewsArticleCommandHandler(
        IApplicationDbContext context,
        ISlugService slugService,
        ITagService tagService,
        ICurrentUser currentUser,
        UserManager<User> userManager)
    {
        _context = context;
        _slugService = slugService;
        _tagService = tagService;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<NewsArticleDto> Handle(CreateNewsArticleCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AuthenticationException("UNAUTHORIZED", "User is not authenticated.");
        }

        var spaceExists = await _context.Spaces
            .AnyAsync(s => s.Id == request.SpaceId, cancellationToken);

        if (!spaceExists)
        {
            throw new NotFoundException("Space", request.SpaceId);
        }

        if (request.ChannelId.HasValue)
        {
            var channelExists = await _context.NewsChannels
                .AnyAsync(c => c.Id == request.ChannelId.Value && c.SpaceId == request.SpaceId, cancellationToken);

            if (!channelExists)
            {
                throw new NotFoundException("NewsChannel", request.ChannelId.Value);
            }
        }

        if (request.GhostAuthorId.HasValue)
        {
            var ghostAuthorExists = await _userManager.FindByIdAsync(request.GhostAuthorId.Value.ToString());
            if (ghostAuthorExists is null)
            {
                throw new NotFoundException("User (GhostAuthor)", request.GhostAuthorId.Value);
            }
        }

        var slug = await GenerateUniqueSlugAsync(request.Title, cancellationToken);

        var status = request.Status;
        DateTimeOffset? publishedAt = null;
        DateTimeOffset? scheduledAt = null;

        if (request.ScheduledAt.HasValue)
        {
            status = ContentStatus.Scheduled;
            scheduledAt = request.ScheduledAt.Value;
        }
        else if (status == ContentStatus.Published)
        {
            publishedAt = DateTimeOffset.UtcNow;
        }

        var article = new NewsArticle
        {
            SpaceId = request.SpaceId,
            ChannelId = request.ChannelId,
            Title = request.Title,
            Slug = slug,
            TeaserText = request.TeaserText,
            TeaserImageUrl = request.TeaserImageUrl,
            Content = request.Content,
            Status = status,
            PublishedAt = publishedAt,
            ScheduledAt = scheduledAt,
            AuthorId = _currentUser.UserId.Value,
            GhostAuthorId = request.GhostAuthorId,
            AllowComments = request.AllowComments,
            IsPinned = request.IsPinned,
            VisibilityRule = request.VisibilityRule,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };

        _context.NewsArticles.Add(article);
        await _context.SaveChangesAsync(cancellationToken);

        // Sync tags
        if (request.Tags.Count > 0)
        {
            await _tagService.SyncTagsAsync(article.Id, request.Tags, cancellationToken);
        }

        var author = await _userManager.FindByIdAsync(_currentUser.UserId.Value.ToString());
        var displayAuthor = request.GhostAuthorId.HasValue
            ? await _userManager.FindByIdAsync(request.GhostAuthorId.Value.ToString())
            : author;

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
            Tags = request.Tags,
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt
        };
    }

    private async Task<string> GenerateUniqueSlugAsync(string title, CancellationToken cancellationToken)
    {
        var baseSlug = _slugService.GenerateSlug(title);
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "article";

        var slug = baseSlug;
        var counter = 1;

        while (await _context.NewsArticles.AnyAsync(a => a.Slug == slug, cancellationToken))
        {
            counter++;
            slug = $"{baseSlug}-{counter}";
        }

        return slug;
    }
}
