using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Spaces.Commands;
using CommonHall.Application.Features.Spaces.Queries;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SpacesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IContentAuthorizationService _authorizationService;
    private readonly ICurrentUser _currentUser;

    public SpacesController(
        IMediator mediator,
        IContentAuthorizationService authorizationService,
        ICurrentUser currentUser)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all spaces with optional filtering
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<SpaceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpaces(
        [FromQuery] Guid? parentSpaceId = null,
        [FromQuery] bool includeChildren = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSpacesQuery
        {
            ParentSpaceId = parentSpaceId,
            IncludeChildren = includeChildren
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<List<SpaceDto>>.Success(result));
    }

    /// <summary>
    /// Get a space by slug
    /// </summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SpaceDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpace(string slug, CancellationToken cancellationToken)
    {
        var query = new GetSpaceBySlugQuery(slug);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<SpaceDetailDto>.Success(result));
    }

    /// <summary>
    /// Create a new space (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(ApiResponse<SpaceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSpace([FromBody] CreateSpaceRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateSpaceCommand
        {
            Name = request.Name,
            Description = request.Description,
            IconUrl = request.IconUrl,
            ParentSpaceId = request.ParentSpaceId
        };

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetSpace),
            new { slug = result.Slug },
            ApiResponse<SpaceDto>.Success(result));
    }

    /// <summary>
    /// Update a space (Admin or Space Admin)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SpaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSpace(Guid id, [FromBody] UpdateSpaceRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canManage = await _authorizationService.CanManageSpaceAsync(_currentUser.UserId.Value, id, cancellationToken);
        if (!canManage)
        {
            return Forbid();
        }

        var command = new UpdateSpaceCommand
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            IconUrl = request.IconUrl,
            CoverImageUrl = request.CoverImageUrl,
            SortOrder = request.SortOrder
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<SpaceDto>.Success(result));
    }

    /// <summary>
    /// Delete a space (Admin or Space Admin)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteSpace(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canManage = await _authorizationService.CanManageSpaceAsync(_currentUser.UserId.Value, id, cancellationToken);
        if (!canManage)
        {
            return Forbid();
        }

        var command = new DeleteSpaceCommand(id);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Add an administrator to a space (Admin or Space Admin)
    /// </summary>
    [HttpPost("{id:guid}/admins")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddSpaceAdmin(Guid id, [FromBody] AddSpaceAdminRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canManage = await _authorizationService.CanManageSpaceAsync(_currentUser.UserId.Value, id, cancellationToken);
        if (!canManage)
        {
            return Forbid();
        }

        var command = new AddSpaceAdminCommand
        {
            SpaceId = id,
            UserId = request.UserId
        };

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Remove an administrator from a space (Admin or Space Admin)
    /// </summary>
    [HttpDelete("{id:guid}/admins/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveSpaceAdmin(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canManage = await _authorizationService.CanManageSpaceAsync(_currentUser.UserId.Value, id, cancellationToken);
        if (!canManage)
        {
            return Forbid();
        }

        var command = new RemoveSpaceAdminCommand
        {
            SpaceId = id,
            UserId = userId
        };

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

public sealed record CreateSpaceRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public Guid? ParentSpaceId { get; init; }
}

public sealed record UpdateSpaceRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public string? CoverImageUrl { get; init; }
    public int? SortOrder { get; init; }
}

public sealed record AddSpaceAdminRequest
{
    public Guid UserId { get; init; }
}
