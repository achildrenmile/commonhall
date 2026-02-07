using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CommonHall.Application.Interfaces;

public interface IAnalyticsService
{
    Task<ContentAnalytics> GetContentAnalyticsAsync(
        string targetType,
        Guid targetId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<OverviewAnalytics> GetOverviewAnalyticsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<SearchAnalytics> GetSearchAnalyticsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}

public record ContentAnalytics(
    int Views,
    int UniqueViewers,
    int Reactions,
    int Comments,
    List<DailyCount> ViewsByDay
);

public record OverviewAnalytics(
    int DailyActiveUsers,
    int MonthlyActiveUsers,
    int PageViews,
    int ArticleViews,
    List<ContentRanking> TopArticles,
    List<ContentRanking> TopPages,
    List<SearchQueryRanking> TopSearches,
    List<SearchQueryRanking> ZeroResultSearches,
    List<ChannelDistribution> ChannelDistribution,
    List<DeviceDistribution> DeviceDistribution,
    List<DailyCount> DailyActiveUsersByDay,
    List<DailyCount> PageViewsByDay
);

public record SearchAnalytics(
    int TotalSearches,
    List<SearchQueryRanking> TopQueries,
    List<SearchQueryRanking> ZeroResultQueries,
    double ClickThroughRate
);

public record DailyCount(DateOnly Date, int Count);
public record ContentRanking(Guid Id, string Title, string? Slug, int Views, int UniqueViewers);
public record SearchQueryRanking(string Query, int Count, int ResultCount);
public record ChannelDistribution(string Channel, int Count);
public record DeviceDistribution(string DeviceType, int Count);
