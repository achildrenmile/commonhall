using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Comments.Commands;
using CommonHall.Application.Features.News.Comments.Queries;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/news/{articleId:guid}/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public CommentsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get comments for a news article with cursor-based pagination
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<CommentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComments(
        Guid articleId,
        [FromQuery] string? cursor = null,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCommentsQuery
        {
            NewsArticleId = articleId,
            Cursor = cursor,
            Size = size
        };

        var result = await _mediator.Send(query, cancellationToken);

        var meta = new ApiMeta
        {
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };

        return Ok(ApiResponse<List<CommentDto>>.Success(result.Items, meta));
    }

    /// <summary>
    /// Add a comment to a news article
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddComment(
        Guid articleId,
        [FromBody] AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddCommentCommand
        {
            NewsArticleId = articleId,
            ParentCommentId = request.ParentCommentId,
            Body = request.Body
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Created(string.Empty, ApiResponse<CommentDto>.Success(result));
    }

    /// <summary>
    /// Delete a comment (author or admin only)
    /// </summary>
    [HttpDelete("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(
        Guid articleId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteCommentCommand(commentId);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Moderate a comment (admin only)
    /// </summary>
    [HttpPost("{commentId:guid}/moderate")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ModerateComment(
        Guid articleId,
        Guid commentId,
        [FromBody] ModerateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ModerateCommentCommand
        {
            Id = commentId,
            IsModerated = request.IsModerated
        };

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

public sealed record AddCommentRequest
{
    public required string Body { get; init; }
    public Guid? ParentCommentId { get; init; }
}

public sealed record ModerateCommentRequest
{
    public required bool IsModerated { get; init; }
}
