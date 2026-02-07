using System.Security.Claims;
using CommonHall.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Federated search across all content types
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? type = null,
        [FromQuery] string? space = null,
        [FromQuery] int from = 0,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new { data = new SearchResult() });
        }

        size = Math.Clamp(size, 1, 100);
        from = Math.Max(0, from);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? parsedUserId = Guid.TryParse(userId, out var uid) ? uid : null;

        var query = new SearchQuery
        {
            Query = q,
            Type = type,
            SpaceSlug = space,
            From = from,
            Size = size,
            CurrentUserId = parsedUserId
        };

        var result = await _searchService.SearchAsync(query, cancellationToken);

        return Ok(new { data = result });
    }

    /// <summary>
    /// Quick search suggestions for autocomplete
    /// </summary>
    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest(
        [FromQuery] string q,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return Ok(new { data = new List<SearchSuggestion>() });
        }

        limit = Math.Clamp(limit, 1, 10);

        var suggestions = await _searchService.SuggestAsync(q, limit, cancellationToken);

        return Ok(new { data = suggestions });
    }
}

[ApiController]
[Route("api/v1/admin/search")]
[Authorize(Roles = "Admin")]
public class SearchAdminController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchAdminController> _logger;

    public SearchAdminController(ISearchService searchService, ILogger<SearchAdminController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Trigger a full reindex of all content
    /// </summary>
    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting full reindex");

        var progress = new Progress<ReindexProgress>(p =>
        {
            _logger.LogInformation("Reindex progress: {Message}", p.Message);
        });

        await _searchService.ReindexAllAsync(progress, cancellationToken);

        return Ok(new { data = new { message = "Reindex complete" } });
    }

    /// <summary>
    /// Ensure search indexes exist with correct mappings
    /// </summary>
    [HttpPost("ensure-indexes")]
    public async Task<IActionResult> EnsureIndexes(CancellationToken cancellationToken)
    {
        await _searchService.EnsureIndexesExistAsync(cancellationToken);
        return Ok(new { data = new { message = "Indexes ensured" } });
    }

    /// <summary>
    /// Index a specific item
    /// </summary>
    [HttpPost("index/{type}/{id:guid}")]
    public async Task<IActionResult> IndexItem(string type, Guid id, CancellationToken cancellationToken)
    {
        switch (type.ToLower())
        {
            case "news":
                await _searchService.IndexNewsArticleAsync(id, cancellationToken);
                break;
            case "pages":
                await _searchService.IndexPageAsync(id, cancellationToken);
                break;
            case "users":
                await _searchService.IndexUserAsync(id, cancellationToken);
                break;
            case "files":
                await _searchService.IndexFileAsync(id, cancellationToken);
                break;
            default:
                return BadRequest(new { error = $"Unknown type: {type}" });
        }

        return Ok(new { data = new { message = $"Indexed {type}/{id}" } });
    }
}
