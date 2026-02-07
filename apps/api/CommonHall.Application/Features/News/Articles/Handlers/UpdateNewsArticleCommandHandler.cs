using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Articles.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Articles.Handlers;

public sealed class UpdateNewsArticleCommandHandler : IRequestHandler<UpdateNewsArticleCommand, NewsArticleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITagService _tagService;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<User> _userManager;

    public UpdateNewsArticleCommandHandler(
        IApplicationDbContext context,
        ITagService tagService,
        ICurrentUser currentUser,
        UserManager<User> userManager)
    {
        _context = context;
        _tagService = tagService;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<NewsArticleDto> Handle(UpdateNewsArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _context.NewsArticles
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (article is null)
        {
            throw new NotFoundException("NewsArticle", request.Id);
        }

        if (request.ChannelId.HasValue)
        {
            var channelExists = await _context.NewsChannels
                .AnyAsync(c => c.Id == request.ChannelId.Value && c.SpaceId == article.SpaceId, cancellationToken);

            if (!channelExists)
            {
                throw new NotFoundException("NewsChannel", request.ChannelId.Value);
            }
            article.ChannelId = request.ChannelId;
        }

        if (request.GhostAuthorId.HasValue)
        {
            var ghostAuthorExists = await _userManager.FindByIdAsync(request.GhostAuthorId.Value.ToString());
            if (ghostAuthorExists is null)
            {
                throw new NotFoundException("User (GhostAuthor)", request.GhostAuthorId.Value);
            }
            article.GhostAuthorId = request.GhostAuthorId;
        }

        if (request.Title is not null) article.Title = request.Title;
        if (request.TeaserText is not null) article.TeaserText = request.TeaserText;
        if (request.TeaserImageUrl is not null) article.TeaserImageUrl = request.TeaserImageUrl;
        if (request.Content is not null) article.Content = request.Content;
        if (request.AllowComments.HasValue) article.AllowComments = request.AllowComments.Value;
        if (request.IsPinned.HasValue) article.IsPinned = request.IsPinned.Value;
        if (request.VisibilityRule is not null) article.VisibilityRule = request.VisibilityRule;

        article.UpdatedBy = _currentUser.UserId;

        // Sync tags if provided
        if (request.Tags is not null)
        {
            await _tagService.SyncTagsAsync(article.Id, request.Tags, cancellationToken);
        }

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
