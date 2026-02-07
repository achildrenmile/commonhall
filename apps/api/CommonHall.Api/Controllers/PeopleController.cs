using CommonHall.Application.Common;
using CommonHall.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PeopleController : ControllerBase
{
    private readonly IMediator _mediator;

    public PeopleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Search users with filtering and facets for employee directory
    /// </summary>
    /// <remarks>
    /// Returns paginated results with department and location facets.
    /// When searching, results are sorted by relevance.
    /// When browsing, results are sorted alphabetically.
    /// </remarks>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<SearchUsersResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery(Name = "q")] string? query = null,
        [FromQuery] string? department = null,
        [FromQuery] string? location = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        // Parse multiple departments/locations from comma-separated values
        var departments = !string.IsNullOrWhiteSpace(department)
            ? department.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : null;

        var locations = !string.IsNullOrWhiteSpace(location)
            ? location.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : null;

        var searchQuery = new SearchUsersQuery
        {
            Query = query,
            Departments = departments,
            Locations = locations,
            Cursor = cursor,
            Size = Math.Min(size, 100) // Cap at 100
        };

        var result = await _mediator.Send(searchQuery, cancellationToken);

        return Ok(ApiResponse<SearchUsersResult>.Success(
            result,
            new ApiMeta
            {
                TotalCount = result.TotalCount,
                HasMore = result.HasNextPage,
                NextCursor = result.NextCursor
            }));
    }

    /// <summary>
    /// Get user profile with groups and recent articles
    /// </summary>
    [HttpGet("{id:guid}/profile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserProfile(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetUserProfileQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<UserProfileDto>.Success(result));
    }

    /// <summary>
    /// Get list of all departments with counts
    /// </summary>
    [HttpGet("departments")]
    [ProducesResponseType(typeof(ApiResponse<List<FacetItem>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
    {
        // Use search with no filters to get all facets
        var searchQuery = new SearchUsersQuery { Size = 0 };
        var result = await _mediator.Send(searchQuery, cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<FacetItem>>.Success(result.Facets.Departments));
    }

    /// <summary>
    /// Get list of all locations with counts
    /// </summary>
    [HttpGet("locations")]
    [ProducesResponseType(typeof(ApiResponse<List<FacetItem>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocations(CancellationToken cancellationToken)
    {
        // Use search with no filters to get all facets
        var searchQuery = new SearchUsersQuery { Size = 0 };
        var result = await _mediator.Send(searchQuery, cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<FacetItem>>.Success(result.Facets.Locations));
    }
}
