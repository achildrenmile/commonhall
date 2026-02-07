using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Channels.Commands;
using CommonHall.Application.Features.News.Channels.Queries;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/news-channels")]
[Authorize]
public class NewsChannelsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IContentAuthorizationService _authorizationService;
    private readonly ICurrentUser _currentUser;

    public NewsChannelsController(
        IMediator mediator,
        IContentAuthorizationService authorizationService,
        ICurrentUser currentUser)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all news channels, optionally filtered by space
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<NewsChannelDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChannels(
        [FromQuery] Guid? spaceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNewsChannelsQuery { SpaceId = spaceId };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<List<NewsChannelDto>>.Success(result));
    }

    /// <summary>
    /// Get a news channel by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<NewsChannelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChannel(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetNewsChannelByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<NewsChannelDto>.Success(result));
    }

    /// <summary>
    /// Create a new news channel (Admin or Space Admin)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<NewsChannelDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateChannel([FromBody] CreateNewsChannelRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canManage = await _authorizationService.CanManageSpaceAsync(_currentUser.UserId.Value, request.SpaceId, cancellationToken);
        if (!canManage)
        {
            return Forbid();
        }

        var command = new CreateNewsChannelCommand
        {
            SpaceId = request.SpaceId,
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            SortOrder = request.SortOrder
        };

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetChannel),
            new { id = result.Id },
            ApiResponse<NewsChannelDto>.Success(result));
    }

    /// <summary>
    /// Update a news channel (Admin or Space Admin)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<NewsChannelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateChannel(Guid id, [FromBody] UpdateNewsChannelRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        // Check authorization based on channel's space
        var channel = await _mediator.Send(new GetNewsChannelByIdQuery(id), cancellationToken);
        var canManage = await _authorizationService.CanManageSpaceAsync(_currentUser.UserId.Value, channel.SpaceId, cancellationToken);
        if (!canManage)
        {
            return Forbid();
        }

        var command = new UpdateNewsChannelCommand
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            SortOrder = request.SortOrder
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<NewsChannelDto>.Success(result));
    }

    /// <summary>
    /// Delete a news channel (Admin or Space Admin)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteChannel(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        // Check authorization based on channel's space
        var channel = await _mediator.Send(new GetNewsChannelByIdQuery(id), cancellationToken);
        var canManage = await _authorizationService.CanManageSpaceAsync(_currentUser.UserId.Value, channel.SpaceId, cancellationToken);
        if (!canManage)
        {
            return Forbid();
        }

        var command = new DeleteNewsChannelCommand(id);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

public sealed record CreateNewsChannelRequest
{
    public required Guid SpaceId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Color { get; init; }
    public int SortOrder { get; init; }
}

public sealed record UpdateNewsChannelRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Color { get; init; }
    public int? SortOrder { get; init; }
}
