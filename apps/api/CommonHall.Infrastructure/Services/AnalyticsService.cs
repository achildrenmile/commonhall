using System.Text.Json;
using CommonHall.Application.Interfaces;
using CommonHall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Infrastructure.Services;

public sealed class AnalyticsService : IAnalyticsService
{
    private readonly CommonHallDbContext _dbContext;

    public AnalyticsService(CommonHallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ContentAnalytics> GetContentAnalyticsAsync(
        string targetType,
        Guid targetId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _dbContext.TrackingEvents
            .Where(e => e.TargetType == targetType && e.TargetId == targetId)
            .Where(e => e.Timestamp >= from && e.Timestamp <= to);

        var viewEvents = baseQuery.Where(e => e.EventType == "article_view" || e.EventType == "page_view");

        var views = await viewEvents.CountAsync(cancellationToken);

        var uniqueViewers = await viewEvents
            .Where(e => e.UserId != null)
            .Select(e => e.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var reactions = await baseQuery
            .Where(e => e.EventType == "article_like")
            .CountAsync(cancellationToken);

        var comments = await baseQuery
            .Where(e => e.EventType == "article_comment")
            .CountAsync(cancellationToken);

        var viewsByDay = await viewEvents
            .GroupBy(e => e.Timestamp.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        return new ContentAnalytics(
            views,
            uniqueViewers,
            reactions,
            comments,
            viewsByDay.Select(x => new DailyCount(DateOnly.FromDateTime(x.Date), x.Count)).ToList()
        );
    }

    public async Task<OverviewAnalytics> GetOverviewAnalyticsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _dbContext.TrackingEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to);

        // DAU - unique users in the last day of the range
        var lastDayStart = to.AddDays(-1);
        var dau = await baseQuery
            .Where(e => e.Timestamp >= lastDayStart && e.UserId != null)
            .Select(e => e.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        // MAU - unique users in the range (up to 30 days)
        var mau = await baseQuery
            .Where(e => e.UserId != null)
            .Select(e => e.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Page views
        var pageViews = await baseQuery
            .Where(e => e.EventType == "page_view")
            .CountAsync(cancellationToken);

        // Article views
        var articleViews = await baseQuery
            .Where(e => e.EventType == "article_view")
            .CountAsync(cancellationToken);

        // Top articles
        var topArticles = await baseQuery
            .Where(e => e.EventType == "article_view" && e.TargetId != null)
            .GroupBy(e => e.TargetId)
            .Select(g => new
            {
                Id = g.Key!.Value,
                Views = g.Count(),
                UniqueViewers = g.Where(x => x.UserId != null).Select(x => x.UserId).Distinct().Count()
            })
            .OrderByDescending(x => x.Views)
            .Take(10)
            .ToListAsync(cancellationToken);

        var articleIds = topArticles.Select(a => a.Id).ToList();
        var articles = await _dbContext.NewsArticles
            .Where(a => articleIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Title, a.Slug })
            .ToListAsync(cancellationToken);

        var topArticlesResult = topArticles.Select(ta =>
        {
            var article = articles.FirstOrDefault(a => a.Id == ta.Id);
            return new ContentRanking(ta.Id, article?.Title ?? "Unknown", article?.Slug, ta.Views, ta.UniqueViewers);
        }).ToList();

        // Top pages
        var topPages = await baseQuery
            .Where(e => e.EventType == "page_view" && e.TargetId != null)
            .GroupBy(e => e.TargetId)
            .Select(g => new
            {
                Id = g.Key!.Value,
                Views = g.Count(),
                UniqueViewers = g.Where(x => x.UserId != null).Select(x => x.UserId).Distinct().Count()
            })
            .OrderByDescending(x => x.Views)
            .Take(10)
            .ToListAsync(cancellationToken);

        var pageIds = topPages.Select(p => p.Id).ToList();
        var pages = await _dbContext.Pages
            .Where(p => pageIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title, p.Slug })
            .ToListAsync(cancellationToken);

        var topPagesResult = topPages.Select(tp =>
        {
            var page = pages.FirstOrDefault(p => p.Id == tp.Id);
            return new ContentRanking(tp.Id, page?.Title ?? "Unknown", page?.Slug, tp.Views, tp.UniqueViewers);
        }).ToList();

        // Top searches
        var searchEvents = await baseQuery
            .Where(e => e.EventType == "search_query" && e.Metadata != null)
            .Select(e => e.Metadata)
            .ToListAsync(cancellationToken);

        var searchQueries = new Dictionary<string, (int Count, int TotalResults)>();
        foreach (var metadata in searchEvents)
        {
            if (string.IsNullOrEmpty(metadata)) continue;
            try
            {
                using var doc = JsonDocument.Parse(metadata);
                if (doc.RootElement.TryGetProperty("query", out var queryProp))
                {
                    var query = queryProp.GetString()?.ToLowerInvariant() ?? "";
                    var resultCount = doc.RootElement.TryGetProperty("resultCount", out var resultProp)
                        ? resultProp.GetInt32()
                        : 0;

                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        if (searchQueries.TryGetValue(query, out var existing))
                        {
                            searchQueries[query] = (existing.Count + 1, existing.TotalResults + resultCount);
                        }
                        else
                        {
                            searchQueries[query] = (1, resultCount);
                        }
                    }
                }
            }
            catch { /* Ignore malformed JSON */ }
        }

        var topSearches = searchQueries
            .OrderByDescending(x => x.Value.Count)
            .Take(10)
            .Select(x => new SearchQueryRanking(x.Key, x.Value.Count, x.Value.TotalResults / x.Value.Count))
            .ToList();

        var zeroResultSearches = searchQueries
            .Where(x => x.Value.TotalResults == 0)
            .OrderByDescending(x => x.Value.Count)
            .Take(10)
            .Select(x => new SearchQueryRanking(x.Key, x.Value.Count, 0))
            .ToList();

        // Channel distribution
        var channelDistribution = await baseQuery
            .Where(e => e.Channel != null)
            .GroupBy(e => e.Channel!)
            .Select(g => new ChannelDistribution(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        // Device distribution
        var deviceDistribution = await baseQuery
            .Where(e => e.DeviceType != null)
            .GroupBy(e => e.DeviceType!)
            .Select(g => new DeviceDistribution(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        // DAU by day
        var dauByDay = await baseQuery
            .Where(e => e.UserId != null)
            .GroupBy(e => e.Timestamp.Date)
            .Select(g => new { Date = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        // Page views by day
        var pageViewsByDay = await baseQuery
            .Where(e => e.EventType == "page_view" || e.EventType == "article_view")
            .GroupBy(e => e.Timestamp.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        return new OverviewAnalytics(
            dau,
            mau,
            pageViews,
            articleViews,
            topArticlesResult,
            topPagesResult,
            topSearches,
            zeroResultSearches,
            channelDistribution,
            deviceDistribution,
            dauByDay.Select(x => new DailyCount(DateOnly.FromDateTime(x.Date), x.Count)).ToList(),
            pageViewsByDay.Select(x => new DailyCount(DateOnly.FromDateTime(x.Date), x.Count)).ToList()
        );
    }

    public async Task<SearchAnalytics> GetSearchAnalyticsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var searchEvents = await _dbContext.TrackingEvents
            .Where(e => e.EventType == "search_query" && e.Timestamp >= from && e.Timestamp <= to)
            .Select(e => e.Metadata)
            .ToListAsync(cancellationToken);

        var searchQueries = new Dictionary<string, (int Count, int TotalResults, int Clicks)>();
        var totalSearches = 0;
        var totalClicks = 0;

        foreach (var metadata in searchEvents)
        {
            if (string.IsNullOrEmpty(metadata)) continue;
            totalSearches++;

            try
            {
                using var doc = JsonDocument.Parse(metadata);
                if (doc.RootElement.TryGetProperty("query", out var queryProp))
                {
                    var query = queryProp.GetString()?.ToLowerInvariant() ?? "";
                    var resultCount = doc.RootElement.TryGetProperty("resultCount", out var resultProp)
                        ? resultProp.GetInt32()
                        : 0;
                    var clicked = doc.RootElement.TryGetProperty("clicked", out var clickProp) && clickProp.GetBoolean();

                    if (clicked) totalClicks++;

                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        if (searchQueries.TryGetValue(query, out var existing))
                        {
                            searchQueries[query] = (
                                existing.Count + 1,
                                existing.TotalResults + resultCount,
                                existing.Clicks + (clicked ? 1 : 0)
                            );
                        }
                        else
                        {
                            searchQueries[query] = (1, resultCount, clicked ? 1 : 0);
                        }
                    }
                }
            }
            catch { /* Ignore malformed JSON */ }
        }

        var topQueries = searchQueries
            .OrderByDescending(x => x.Value.Count)
            .Take(20)
            .Select(x => new SearchQueryRanking(x.Key, x.Value.Count, x.Value.TotalResults / x.Value.Count))
            .ToList();

        var zeroResultQueries = searchQueries
            .Where(x => x.Value.TotalResults == 0)
            .OrderByDescending(x => x.Value.Count)
            .Take(20)
            .Select(x => new SearchQueryRanking(x.Key, x.Value.Count, 0))
            .ToList();

        var ctr = totalSearches > 0 ? (double)totalClicks / totalSearches * 100 : 0;

        return new SearchAnalytics(
            totalSearches,
            topQueries,
            zeroResultQueries,
            Math.Round(ctr, 2)
        );
    }
}
