using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Users.Commands;
using CommonHall.Application.Users.Queries;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireAdminRole")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all users with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? department = null,
        [FromQuery] string? location = null,
        [FromQuery] string? role = null,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersQuery
        {
            Search = search,
            Department = department,
            Location = location,
            Role = role,
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
    /// Get a user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<UserDto>.Success(result));
    }

    /// <summary>
    /// Update a user
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand
        {
            Id = id,
            DisplayName = request.DisplayName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Department = request.Department,
            Location = request.Location,
            JobTitle = request.JobTitle,
            Role = request.Role,
            IsActive = request.IsActive
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<UserDto>.Success(result));
    }

    /// <summary>
    /// Soft delete a user
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteUserCommand(id);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Activate a user
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(Guid id, CancellationToken cancellationToken)
    {
        var command = new ActivateUserCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<UserDto>.Success(result));
    }

    /// <summary>
    /// Get groups that a user belongs to
    /// </summary>
    [HttpGet("{id:guid}/groups")]
    [ProducesResponseType(typeof(ApiResponse<List<UserGroupDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserGroups(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetUserGroupsQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<List<UserGroupDto>>.Success(result));
    }
}

public sealed record UpdateUserRequest
{
    public string? DisplayName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Department { get; init; }
    public string? Location { get; init; }
    public string? JobTitle { get; init; }
    public UserRole? Role { get; init; }
    public bool? IsActive { get; init; }
}
