using CommonHall.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/admin/content-health")]
[Authorize(Roles = "Admin,Editor")]
public class ContentHealthController : ControllerBase
{
    private readonly IContentHealthService _contentHealthService;
    private readonly ILogger<ContentHealthController> _logger;

    public ContentHealthController(
        IContentHealthService contentHealthService,
        ILogger<ContentHealthController> logger)
    {
        _contentHealthService = contentHealthService;
        _logger = logger;
    }

    /// <summary>
    /// Start a new content health scan
    /// </summary>
    [HttpPost("scan")]
    public async Task<IActionResult> StartScan(CancellationToken cancellationToken)
    {
        var reportId = await _contentHealthService.StartScanAsync(cancellationToken);
        return Ok(new { data = new { reportId } });
    }

    /// <summary>
    /// Get a specific content health report
    /// </summary>
    [HttpGet("reports/{reportId:guid}")]
    public async Task<IActionResult> GetReport(Guid reportId, CancellationToken cancellationToken)
    {
        var report = await _contentHealthService.GetReportAsync(reportId, cancellationToken);

        if (report == null)
        {
            return NotFound(new { error = "Report not found" });
        }

        return Ok(new { data = report });
    }

    /// <summary>
    /// Get the latest content health report
    /// </summary>
    [HttpGet("reports/latest")]
    public async Task<IActionResult> GetLatestReport(CancellationToken cancellationToken)
    {
        var report = await _contentHealthService.GetLatestReportAsync(cancellationToken);

        if (report == null)
        {
            return Ok(new { data = (object?)null, message = "No reports found. Start a scan to generate one." });
        }

        return Ok(new { data = report });
    }

    /// <summary>
    /// Get report history
    /// </summary>
    [HttpGet("reports")]
    public async Task<IActionResult> GetReportHistory(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var reports = await _contentHealthService.GetReportHistoryAsync(limit, cancellationToken);
        return Ok(new { data = reports });
    }

    /// <summary>
    /// Mark an issue as resolved
    /// </summary>
    [HttpPost("issues/{issueId:guid}/resolve")]
    public async Task<IActionResult> ResolveIssue(Guid issueId, CancellationToken cancellationToken)
    {
        // Get current user ID from claims
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        await _contentHealthService.ResolveIssueAsync(issueId, userGuid, cancellationToken);
        return Ok(new { message = "Issue marked as resolved" });
    }
}
