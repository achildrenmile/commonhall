using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Collections.Commands;
using CommonHall.Application.Features.Collections.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/collections")]
[Authorize]
public class CollectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CollectionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// List all collections with optional filtering
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<FileCollectionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCollections(
        [FromQuery] Guid? spaceId = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListCollectionsQuery
        {
            SpaceId = spaceId,
            Search = search
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<List<FileCollectionDto>>.Success(result));
    }

    /// <summary>
    /// Get a collection by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<FileCollectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCollection(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetCollectionQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<FileCollectionDto>.Success(result));
    }

    /// <summary>
    /// Create a new collection
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FileCollectionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCollectionCommand
        {
            Name = request.Name,
            Description = request.Description,
            SpaceId = request.SpaceId
        };

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetCollection),
            new { id = result.Id },
            ApiResponse<FileCollectionDto>.Success(result));
    }

    /// <summary>
    /// Update a collection
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FileCollectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCollection(Guid id, [FromBody] UpdateCollectionRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCollectionCommand
        {
            Id = id,
            Name = request.Name,
            Description = request.Description
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<FileCollectionDto>.Success(result));
    }

    /// <summary>
    /// Delete a collection
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCollection(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteCollectionCommand(id);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

public sealed record CreateCollectionRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Guid? SpaceId { get; init; }
}

public sealed record UpdateCollectionRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
}
