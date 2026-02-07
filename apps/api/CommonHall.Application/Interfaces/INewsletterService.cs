using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;

namespace CommonHall.Application.Interfaces;

/// <summary>
/// Service for managing and sending newsletters.
/// </summary>
public interface INewsletterService
{
    /// <summary>
    /// Queues a newsletter for sending.
    /// </summary>
    Task<bool> QueueForSendingAsync(Guid newsletterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a test email to a single recipient.
    /// </summary>
    Task<bool> SendTestAsync(Guid newsletterId, string testEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a newsletter for future sending.
    /// </summary>
    Task<bool> ScheduleAsync(Guid newsletterId, DateTimeOffset scheduledAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets newsletter analytics.
    /// </summary>
    Task<NewsletterAnalytics> GetAnalyticsAsync(Guid newsletterId, CancellationToken cancellationToken = default);
}

public record NewsletterAnalytics
{
    public int TotalRecipients { get; init; }
    public int Sent { get; init; }
    public int Delivered { get; init; }
    public int Opened { get; init; }
    public int Clicked { get; init; }
    public int Bounced { get; init; }
    public decimal OpenRate { get; init; }
    public decimal ClickRate { get; init; }
    public decimal ClickToOpenRate { get; init; }
    public IReadOnlyList<LinkAnalytics> TopLinks { get; init; } = Array.Empty<LinkAnalytics>();
    public IReadOnlyList<TimeSeriesPoint> OpenTimeline { get; init; } = Array.Empty<TimeSeriesPoint>();
    public IReadOnlyList<DeviceStats> DeviceBreakdown { get; init; } = Array.Empty<DeviceStats>();
}

public record LinkAnalytics(string Url, int Clicks, int UniqueClicks);

public record TimeSeriesPoint(DateTimeOffset Time, int Value);

public record DeviceStats(string Device, int Count, decimal Percentage);
