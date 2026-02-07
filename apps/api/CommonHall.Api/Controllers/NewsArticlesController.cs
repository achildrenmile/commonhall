using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Articles.Commands;
using CommonHall.Application.Features.News.Articles.Queries;
using CommonHall.Application.Features.News.Reactions.Commands;
using CommonHall.Application.Features.News.Reactions.Queries;
using CommonHall.Application.Features.News.Tags.Queries;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/news")]
[Authorize]
public class NewsArticlesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IContentAuthorizationService _authorizationService;
    private readonly ICurrentUser _currentUser;

    public NewsArticlesController(
        IMediator mediator,
        IContentAuthorizationService authorizationService,
        ICurrentUser currentUser)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get news feed with optional filters and cursor-based pagination
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<NewsArticleListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNewsFeed(
        [FromQuery] string? spaceSlug = null,
        [FromQuery] string? channelSlug = null,
        [FromQuery] string? tagSlug = null,
        [FromQuery] ArticleStatus? status = null,
        [FromQuery] bool? isPinned = null,
        [FromQuery] string? search = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNewsFeedQuery
        {
            SpaceSlug = spaceSlug,
            ChannelSlug = channelSlug,
            TagSlug = tagSlug,
            Status = status,
            IsPinned = isPinned,
            Search = search,
            Cursor = cursor,
            Size = size
        };

        var result = await _mediator.Send(query, cancellationToken);

        var meta = new ApiMeta
        {
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };

        return Ok(ApiResponse<List<NewsArticleListDto>>.Success(result.Items, meta));
    }

    /// <summary>
    /// Get a news article by slug
    /// </summary>
    [HttpGet("{spaceSlug}/{articleSlug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<NewsArticleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticle(
        string spaceSlug,
        string articleSlug,
        [FromQuery] bool incrementView = true,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNewsArticleBySlugQuery
        {
            SpaceSlug = spaceSlug,
            ArticleSlug = articleSlug,
            IncrementView = incrementView
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<NewsArticleDetailDto>.Success(result));
    }

    /// <summary>
    /// Create a new news article (Admin or Space Admin)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<NewsArticleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateArticle([FromBody] CreateNewsArticleRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canManage = await _authorizationService.CanManageSpaceAsync(_currentUser.UserId.Value, request.SpaceId, cancellationToken);
        if (!canManage)
        {
            return Forbid();
        }

        var command = new CreateNewsArticleCommand
        {
            SpaceId = request.SpaceId,
            ChannelId = request.ChannelId,
            Title = request.Title,
            TeaserText = request.TeaserText,
            TeaserImageUrl = request.TeaserImageUrl,
            Content = request.Content,
            Tags = request.Tags ?? [],
            GhostAuthorId = request.GhostAuthorId,
            IsPinned = request.IsPinned,
            AllowComments = request.AllowComments,
            ScheduledAt = request.ScheduledAt
        };

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetArticle),
            new { spaceSlug = "space", articleSlug = result.Slug }, // Will be overridden by actual data
            ApiResponse<NewsArticleDto>.Success(result));
    }

    /// <summary>
    /// Update a news article (Admin or Space Admin)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<NewsArticleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateArticle(Guid id, [FromBody] UpdateNewsArticleRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var command = new UpdateNewsArticleCommand
        {
            Id = id,
            ChannelId = request.ChannelId,
            Title = request.Title,
            TeaserText = request.TeaserText,
            TeaserImageUrl = request.TeaserImageUrl,
            Content = request.Content,
            Tags = request.Tags,
            GhostAuthorId = request.GhostAuthorId,
            AllowComments = request.AllowComments
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<NewsArticleDto>.Success(result));
    }

    /// <summary>
    /// Publish a news article immediately (Admin or Space Admin)
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PublishArticle(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var command = new PublishNewsArticleCommand(id);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Schedule a news article for future publication (Admin or Space Admin)
    /// </summary>
    [HttpPost("{id:guid}/schedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ScheduleArticle(Guid id, [FromBody] ScheduleNewsArticleRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var command = new ScheduleNewsArticleCommand
        {
            Id = id,
            ScheduledAt = request.ScheduledAt
        };

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Pin or unpin a news article (Admin or Space Admin)
    /// </summary>
    [HttpPost("{id:guid}/pin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PinArticle(Guid id, [FromBody] PinNewsArticleRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var command = new PinNewsArticleCommand
        {
            Id = id,
            IsPinned = request.IsPinned
        };

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Delete a news article (Admin or Space Admin)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteArticle(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var command = new DeleteNewsArticleCommand(id);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Toggle reaction (like) on an article
    /// </summary>
    [HttpPost("{id:guid}/reactions")]
    [ProducesResponseType(typeof(ApiResponse<ToggleReactionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleReaction(Guid id, [FromBody] ToggleReactionRequest? request = null, CancellationToken cancellationToken = default)
    {
        var command = new ToggleReactionCommand
        {
            NewsArticleId = id,
            Type = request?.Type ?? ReactionType.Like
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<ToggleReactionResultDto>.Success(result));
    }

    /// <summary>
    /// Get reaction summary for an article
    /// </summary>
    [HttpGet("{id:guid}/reactions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ReactionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReactions(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetReactionSummaryQuery { NewsArticleId = id };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<ReactionSummaryDto>.Success(result));
    }

    /// <summary>
    /// Search tags for autocomplete
    /// </summary>
    [HttpGet("tags")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<TagDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTags(
        [FromQuery] string? search = null,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchTagsQuery
        {
            Search = search,
            Limit = limit
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<List<TagDto>>.Success(result));
    }
}

public sealed record CreateNewsArticleRequest
{
    public required Guid SpaceId { get; init; }
    public Guid? ChannelId { get; init; }
    public required string Title { get; init; }
    public string? TeaserText { get; init; }
    public string? TeaserImageUrl { get; init; }
    public required string Content { get; init; }
    public List<string>? Tags { get; init; }
    public Guid? GhostAuthorId { get; init; }
    public bool IsPinned { get; init; }
    public bool AllowComments { get; init; } = true;
    public DateTimeOffset? ScheduledAt { get; init; }
}

public sealed record UpdateNewsArticleRequest
{
    public Guid? ChannelId { get; init; }
    public string? Title { get; init; }
    public string? TeaserText { get; init; }
    public string? TeaserImageUrl { get; init; }
    public string? Content { get; init; }
    public List<string>? Tags { get; init; }
    public Guid? GhostAuthorId { get; init; }
    public bool? AllowComments { get; init; }
}

public sealed record ScheduleNewsArticleRequest
{
    public required DateTimeOffset ScheduledAt { get; init; }
}

public sealed record PinNewsArticleRequest
{
    public required bool IsPinned { get; init; }
}

public sealed record ToggleReactionRequest
{
    public ReactionType Type { get; init; } = ReactionType.Like;
}
