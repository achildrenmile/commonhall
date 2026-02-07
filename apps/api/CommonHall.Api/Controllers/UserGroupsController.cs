using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.UserGroups.Commands;
using CommonHall.Application.UserGroups.Queries;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/groups")]
[Authorize(Policy = "RequireAdminRole")]
public class UserGroupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserGroupsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all user groups with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserGroupDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroups(
        [FromQuery] string? search = null,
        [FromQuery] GroupType? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetGroupsQuery
        {
            Search = search,
            Type = type,
            Page = page,
            Size = size
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<PagedResult<UserGroupDto>>.Success(
            result,
            new ApiMeta
            {
                Page = result.Page,
                Size = result.Size,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages
            }));
    }

    /// <summary>
    /// Get a user group by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroup(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetGroupByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<UserGroupDto>.Success(result));
    }

    /// <summary>
    /// Create a new user group
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserGroupDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateGroupCommand
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            RuleDefinition = request.RuleDefinition
        };

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetGroup),
            new { id = result.Id },
            ApiResponse<UserGroupDto>.Success(result));
    }

    /// <summary>
    /// Update a user group
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] UpdateGroupRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateGroupCommand
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            RuleDefinition = request.RuleDefinition
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<UserGroupDto>.Success(result));
    }

    /// <summary>
    /// Delete a user group
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteGroupCommand(id);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Get members of a user group
    /// </summary>
    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupMembers(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetGroupMembersQuery
        {
            GroupId = id,
            Page = page,
            Size = size
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<PagedResult<UserDto>>.Success(
            result,
            new ApiMeta
            {
                Page = result.Page,
                Size = result.Size,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages
            }));
    }

    /// <summary>
    /// Add members to a user group
    /// </summary>
    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddGroupMembers(Guid id, [FromBody] AddGroupMembersRequest request, CancellationToken cancellationToken)
    {
        var command = new AddGroupMembersCommand
        {
            GroupId = id,
            UserIds = request.UserIds
        };

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Remove a member from a user group
    /// </summary>
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveGroupMember(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var command = new RemoveGroupMemberCommand(id, userId);

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

public sealed record CreateGroupRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public GroupType Type { get; init; } = GroupType.Manual;
    public string? RuleDefinition { get; init; }
}

public sealed record UpdateGroupRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? RuleDefinition { get; init; }
}

public sealed record AddGroupMembersRequest
{
    public required List<Guid> UserIds { get; init; }
}
