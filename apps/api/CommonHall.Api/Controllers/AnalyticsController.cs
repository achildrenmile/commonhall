using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly TrackingEventChannel _eventChannel;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        TrackingEventChannel eventChannel,
        IAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _eventChannel = eventChannel;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Track analytics events (fire-and-forget batch processing)
    /// </summary>
    [HttpPost("track")]
    [AllowAnonymous]
    public IActionResult Track([FromBody] TrackEventsRequest request)
    {
        if (request.Events == null || request.Events.Count == 0)
            return BadRequest(new { error = "No events provided" });

        if (request.Events.Count > 50)
            return BadRequest(new { error = "Maximum 50 events per request" });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? parsedUserId = Guid.TryParse(userId, out var uid) ? uid : null;

        var now = DateTimeOffset.UtcNow;

        foreach (var evt in request.Events)
        {
            if (string.IsNullOrWhiteSpace(evt.EventType) || evt.EventType.Length > 50)
                continue;

            var trackingEvent = new TrackingEvent
            {
                Id = Guid.NewGuid(),
                UserId = parsedUserId,
                EventType = evt.EventType,
                TargetType = evt.TargetType?.Length <= 50 ? evt.TargetType : null,
                TargetId = evt.TargetId,
                Metadata = evt.Metadata != null ? JsonSerializer.Serialize(evt.Metadata) : null,
                Channel = evt.Channel?.Length <= 50 ? evt.Channel : "web",
                DeviceType = evt.DeviceType?.Length <= 50 ? evt.DeviceType : null,
                SessionId = evt.SessionId?.Length <= 100 ? evt.SessionId : null,
                Timestamp = evt.Timestamp ?? now
            };

            // Fire and forget - write to channel, don't wait
            _eventChannel.Writer.TryWrite(trackingEvent);
        }

        return Accepted(new { message = "Events queued for processing" });
    }

    /// <summary>
    /// Get overview analytics (Admin only)
    /// </summary>
    [HttpGet("overview")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var toDate = to ?? DateTimeOffset.UtcNow;
        var fromDate = from ?? toDate.AddDays(-30);

        var analytics = await _analyticsService.GetOverviewAnalyticsAsync(fromDate, toDate, cancellationToken);

        return Ok(new { data = analytics });
    }

    /// <summary>
    /// Get content-specific analytics (Editor+)
    /// </summary>
    [HttpGet("content/{targetType}/{targetId:guid}")]
    [Authorize(Roles = "Editor,Admin")]
    public async Task<IActionResult> GetContentAnalytics(
        string targetType,
        Guid targetId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var toDate = to ?? DateTimeOffset.UtcNow;
        var fromDate = from ?? toDate.AddDays(-30);

        var analytics = await _analyticsService.GetContentAnalyticsAsync(
            targetType, targetId, fromDate, toDate, cancellationToken);

        return Ok(new { data = analytics });
    }

    /// <summary>
    /// Get search analytics (Admin only)
    /// </summary>
    [HttpGet("search")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSearchAnalytics(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var toDate = to ?? DateTimeOffset.UtcNow;
        var fromDate = from ?? toDate.AddDays(-30);

        var analytics = await _analyticsService.GetSearchAnalyticsAsync(fromDate, toDate, cancellationToken);

        return Ok(new { data = analytics });
    }

    /// <summary>
    /// Export analytics data as CSV
    /// </summary>
    [HttpGet("export")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportAnalytics(
        [FromQuery] string type = "overview",
        [FromQuery] string format = "csv",
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        if (format != "csv")
            return BadRequest(new { error = "Only CSV format is supported" });

        var toDate = to ?? DateTimeOffset.UtcNow;
        var fromDate = from ?? toDate.AddDays(-30);

        var csv = new StringBuilder();

        switch (type.ToLower())
        {
            case "overview":
                var overview = await _analyticsService.GetOverviewAnalyticsAsync(fromDate, toDate, cancellationToken);
                csv.AppendLine("Metric,Value");
                csv.AppendLine($"Daily Active Users,{overview.DailyActiveUsers}");
                csv.AppendLine($"Monthly Active Users,{overview.MonthlyActiveUsers}");
                csv.AppendLine($"Page Views,{overview.PageViews}");
                csv.AppendLine($"Article Views,{overview.ArticleViews}");
                csv.AppendLine();
                csv.AppendLine("Top Articles");
                csv.AppendLine("Title,Slug,Views,Unique Viewers");
                foreach (var article in overview.TopArticles)
                {
                    csv.AppendLine($"\"{EscapeCsv(article.Title)}\",{article.Slug},{article.Views},{article.UniqueViewers}");
                }
                csv.AppendLine();
                csv.AppendLine("Top Pages");
                csv.AppendLine("Title,Slug,Views,Unique Viewers");
                foreach (var page in overview.TopPages)
                {
                    csv.AppendLine($"\"{EscapeCsv(page.Title)}\",{page.Slug},{page.Views},{page.UniqueViewers}");
                }
                csv.AppendLine();
                csv.AppendLine("Daily Active Users by Day");
                csv.AppendLine("Date,Count");
                foreach (var day in overview.DailyActiveUsersByDay)
                {
                    csv.AppendLine($"{day.Date:yyyy-MM-dd},{day.Count}");
                }
                break;

            case "search":
                var search = await _analyticsService.GetSearchAnalyticsAsync(fromDate, toDate, cancellationToken);
                csv.AppendLine("Search Analytics");
                csv.AppendLine($"Total Searches,{search.TotalSearches}");
                csv.AppendLine($"Click-Through Rate,{search.ClickThroughRate}%");
                csv.AppendLine();
                csv.AppendLine("Top Queries");
                csv.AppendLine("Query,Count,Avg Results");
                foreach (var query in search.TopQueries)
                {
                    csv.AppendLine($"\"{EscapeCsv(query.Query)}\",{query.Count},{query.ResultCount}");
                }
                csv.AppendLine();
                csv.AppendLine("Zero Result Queries");
                csv.AppendLine("Query,Count");
                foreach (var query in search.ZeroResultQueries)
                {
                    csv.AppendLine($"\"{EscapeCsv(query.Query)}\",{query.Count}");
                }
                break;

            default:
                return BadRequest(new { error = "Invalid export type. Use 'overview' or 'search'" });
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"analytics-{type}-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.csv");
    }

    private static string EscapeCsv(string value)
    {
        return value?.Replace("\"", "\"\"") ?? "";
    }
}

public record TrackEventsRequest(List<TrackEventDto> Events);

public record TrackEventDto(
    string EventType,
    string? TargetType = null,
    Guid? TargetId = null,
    Dictionary<string, object>? Metadata = null,
    string? Channel = null,
    string? DeviceType = null,
    string? SessionId = null,
    DateTimeOffset? Timestamp = null
);
