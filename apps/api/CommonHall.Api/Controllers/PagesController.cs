using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Pages.Commands;
using CommonHall.Application.Features.Pages.Queries;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PagesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IContentAuthorizationService _authorizationService;
    private readonly ICurrentUser _currentUser;

    public PagesController(
        IMediator mediator,
        IContentAuthorizationService authorizationService,
        ICurrentUser currentUser)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get a page by space and page slug
    /// </summary>
    [HttpGet("{spaceSlug}/{pageSlug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PageDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPage(string spaceSlug, string pageSlug, CancellationToken cancellationToken)
    {
        var query = new GetPageBySlugQuery(spaceSlug, pageSlug);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<PageDetailDto>.Success(result));
    }

    /// <summary>
    /// Get pages by space
    /// </summary>
    [HttpGet("space/{spaceId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<PageListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPagesBySpace(
        Guid spaceId,
        [FromQuery] ContentStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPagesBySpaceQuery
        {
            SpaceId = spaceId,
            Status = status
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<List<PageListDto>>.Success(result));
    }

    /// <summary>
    /// Create a new page (Admin, Space Admin, or Editor)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePage([FromBody] CreatePageRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canEdit = await _authorizationService.CanEditContentAsync(_currentUser.UserId.Value, request.SpaceId, cancellationToken);
        if (!canEdit)
        {
            return Forbid();
        }

        var command = new CreatePageCommand
        {
            SpaceId = request.SpaceId,
            Title = request.Title,
            Content = request.Content,
            Status = request.Status,
            VisibilityRule = request.VisibilityRule
        };

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetPage),
            new { spaceSlug = result.SpaceSlug, pageSlug = result.Slug },
            ApiResponse<PageDto>.Success(result));
    }

    /// <summary>
    /// Update a page (Admin, Space Admin, or creator)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdatePage(Guid id, [FromBody] UpdatePageRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canEditBySpace = await CanEditPageAsync(_currentUser.UserId.Value, id, cancellationToken);
        if (!canEditBySpace)
        {
            return Forbid();
        }

        var command = new UpdatePageCommand
        {
            Id = id,
            Title = request.Title,
            Content = request.Content,
            MetaDescription = request.MetaDescription,
            VisibilityRule = request.VisibilityRule
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<PageDto>.Success(result));
    }

    /// <summary>
    /// Publish a page (Admin, Space Admin, or creator)
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PublishPage(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canEdit = await CanEditPageAsync(_currentUser.UserId.Value, id, cancellationToken);
        if (!canEdit)
        {
            return Forbid();
        }

        var command = new PublishPageCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<PageDto>.Success(result));
    }

    /// <summary>
    /// Unpublish a page (Admin, Space Admin, or creator)
    /// </summary>
    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnpublishPage(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canEdit = await CanEditPageAsync(_currentUser.UserId.Value, id, cancellationToken);
        if (!canEdit)
        {
            return Forbid();
        }

        var command = new UnpublishPageCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<PageDto>.Success(result));
    }

    /// <summary>
    /// Delete a page (Admin or Space Admin)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePage(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canEdit = await CanEditPageAsync(_currentUser.UserId.Value, id, cancellationToken);
        if (!canEdit)
        {
            return Forbid();
        }

        var command = new DeletePageCommand(id);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Reorder pages in a space (Admin or Space Admin)
    /// </summary>
    [HttpPut("space/{spaceId:guid}/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReorderPages(Guid spaceId, [FromBody] ReorderPagesRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canManage = await _authorizationService.CanManageSpaceAsync(_currentUser.UserId.Value, spaceId, cancellationToken);
        if (!canManage)
        {
            return Forbid();
        }

        var command = new ReorderPagesCommand
        {
            SpaceId = spaceId,
            Pages = request.Pages.Select(p => new PageOrderItem { Id = p.Id, SortOrder = p.SortOrder }).ToList()
        };

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Set home page for a space (Admin or Space Admin)
    /// </summary>
    [HttpPost("space/{spaceId:guid}/homepage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetHomePage(Guid spaceId, [FromBody] SetHomePageRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canManage = await _authorizationService.CanManageSpaceAsync(_currentUser.UserId.Value, spaceId, cancellationToken);
        if (!canManage)
        {
            return Forbid();
        }

        var command = new SetHomePageCommand
        {
            SpaceId = spaceId,
            PageId = request.PageId
        };

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Get page versions
    /// </summary>
    [HttpGet("{id:guid}/versions")]
    [ProducesResponseType(typeof(ApiResponse<List<PageVersionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPageVersions(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetPageVersionsQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<List<PageVersionDto>>.Success(result));
    }

    /// <summary>
    /// Restore a page version (Admin, Space Admin, or creator)
    /// </summary>
    [HttpPost("{id:guid}/versions/{versionId:guid}/restore")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RestorePageVersion(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Forbid();

        var canEdit = await CanEditPageAsync(_currentUser.UserId.Value, id, cancellationToken);
        if (!canEdit)
        {
            return Forbid();
        }

        var command = new RestorePageVersionCommand
        {
            PageId = id,
            VersionId = versionId
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<PageDto>.Success(result));
    }

    private async Task<bool> CanEditPageAsync(Guid userId, Guid pageId, CancellationToken cancellationToken)
    {
        // Check if user is page creator
        var isCreator = await _authorizationService.IsPageCreatorAsync(userId, pageId, cancellationToken);
        if (isCreator)
            return true;

        // Get page's space and check space permissions
        var context = HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
        var page = await context.Pages
            .Where(p => p.Id == pageId)
            .Select(p => new { p.SpaceId })
            .FirstOrDefaultAsync(cancellationToken);

        if (page is null)
            return false;

        return await _authorizationService.CanEditContentAsync(userId, page.SpaceId, cancellationToken);
    }
}

public sealed record CreatePageRequest
{
    public Guid SpaceId { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public ContentStatus Status { get; init; } = ContentStatus.Draft;
    public string? VisibilityRule { get; init; }
}

public sealed record UpdatePageRequest
{
    public string? Title { get; init; }
    public string? Content { get; init; }
    public string? MetaDescription { get; init; }
    public string? VisibilityRule { get; init; }
}

public sealed record ReorderPagesRequest
{
    public required List<PageOrderItemRequest> Pages { get; init; }
}

public sealed record PageOrderItemRequest
{
    public Guid Id { get; init; }
    public int SortOrder { get; init; }
}

public sealed record SetHomePageRequest
{
    public Guid PageId { get; init; }
}
