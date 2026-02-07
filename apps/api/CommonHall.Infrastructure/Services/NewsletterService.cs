using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Channels;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.Services;

/// <summary>
/// Implementation of INewsletterService with background queue processing.
/// </summary>
public sealed class NewsletterService : INewsletterService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailRenderer _emailRenderer;
    private readonly IEmailSendingService _emailSendingService;
    private readonly Channel<NewsletterSendJob> _sendQueue;
    private readonly ILogger<NewsletterService> _logger;
    private readonly string _baseUrl;

    private const int BatchSize = 100;

    public NewsletterService(
        IApplicationDbContext context,
        IEmailRenderer emailRenderer,
        IEmailSendingService emailSendingService,
        Channel<NewsletterSendJob> sendQueue,
        IConfiguration configuration,
        ILogger<NewsletterService> logger)
    {
        _context = context;
        _emailRenderer = emailRenderer;
        _emailSendingService = emailSendingService;
        _sendQueue = sendQueue;
        _logger = logger;
        _baseUrl = configuration["App:BaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<bool> QueueForSendingAsync(Guid newsletterId, CancellationToken cancellationToken = default)
    {
        var newsletter = await _context.EmailNewsletters
            .FirstOrDefaultAsync(n => n.Id == newsletterId && !n.IsDeleted, cancellationToken);

        if (newsletter == null)
        {
            _logger.LogWarning("Newsletter {Id} not found", newsletterId);
            return false;
        }

        if (newsletter.Status != NewsletterStatus.Draft && newsletter.Status != NewsletterStatus.Scheduled)
        {
            _logger.LogWarning("Newsletter {Id} is not in Draft or Scheduled status", newsletterId);
            return false;
        }

        // Create recipients
        var recipients = await ResolveRecipientsAsync(newsletter, cancellationToken);
        if (!recipients.Any())
        {
            _logger.LogWarning("No recipients found for newsletter {Id}", newsletterId);
            return false;
        }

        // Create recipient records
        foreach (var recipient in recipients)
        {
            _context.EmailRecipients.Add(recipient);
        }

        newsletter.Status = NewsletterStatus.Sending;
        await _context.SaveChangesAsync(cancellationToken);

        // Queue the send job
        await _sendQueue.Writer.WriteAsync(
            new NewsletterSendJob(newsletterId),
            cancellationToken);

        _logger.LogInformation("Newsletter {Id} queued for sending with {Count} recipients",
            newsletterId, recipients.Count);

        return true;
    }

    public async Task<bool> SendTestAsync(Guid newsletterId, string testEmail, CancellationToken cancellationToken = default)
    {
        var newsletter = await _context.EmailNewsletters
            .FirstOrDefaultAsync(n => n.Id == newsletterId && !n.IsDeleted, cancellationToken);

        if (newsletter == null)
            return false;

        // Create a temporary recipient for preview
        var testRecipient = new EmailRecipient
        {
            NewsletterId = newsletterId,
            Email = testEmail,
            TrackingToken = GenerateTrackingToken(),
            Status = EmailRecipientStatus.Pending
        };

        var html = await _emailRenderer.RenderToHtmlAsync(newsletter, testRecipient, _baseUrl, cancellationToken);

        var result = await _emailSendingService.SendAsync(
            testEmail,
            $"[TEST] {newsletter.Subject}",
            html,
            cancellationToken: cancellationToken);

        return result.Success;
    }

    public async Task<bool> ScheduleAsync(Guid newsletterId, DateTimeOffset scheduledAt, CancellationToken cancellationToken = default)
    {
        var newsletter = await _context.EmailNewsletters
            .FirstOrDefaultAsync(n => n.Id == newsletterId && !n.IsDeleted, cancellationToken);

        if (newsletter == null)
            return false;

        if (newsletter.Status != NewsletterStatus.Draft)
            return false;

        newsletter.Status = NewsletterStatus.Scheduled;
        newsletter.ScheduledAt = scheduledAt;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<NewsletterAnalytics> GetAnalyticsAsync(Guid newsletterId, CancellationToken cancellationToken = default)
    {
        var recipients = await _context.EmailRecipients
            .Where(r => r.NewsletterId == newsletterId)
            .Include(r => r.Clicks)
            .ToListAsync(cancellationToken);

        var totalRecipients = recipients.Count;
        var sent = recipients.Count(r => r.Status >= EmailRecipientStatus.Sent);
        var delivered = recipients.Count(r => r.Status == EmailRecipientStatus.Delivered);
        var opened = recipients.Count(r => r.OpenedAt.HasValue);
        var clicked = recipients.Count(r => r.ClickedAt.HasValue);
        var bounced = recipients.Count(r => r.Status == EmailRecipientStatus.Bounced);

        var openRate = sent > 0 ? (decimal)opened / sent * 100 : 0;
        var clickRate = sent > 0 ? (decimal)clicked / sent * 100 : 0;
        var clickToOpenRate = opened > 0 ? (decimal)clicked / opened * 100 : 0;

        // Top links
        var topLinks = recipients
            .SelectMany(r => r.Clicks)
            .GroupBy(c => c.Url)
            .Select(g => new LinkAnalytics(
                g.Key,
                g.Count(),
                g.Select(c => c.RecipientId).Distinct().Count()))
            .OrderByDescending(l => l.Clicks)
            .Take(10)
            .ToList();

        // Open timeline (hourly for the first 48 hours)
        var firstOpen = recipients.Where(r => r.OpenedAt.HasValue).MinBy(r => r.OpenedAt)?.OpenedAt;
        var openTimeline = new List<TimeSeriesPoint>();

        if (firstOpen.HasValue)
        {
            var startTime = firstOpen.Value.ToOffset(TimeSpan.Zero);
            for (var i = 0; i < 48; i++)
            {
                var hour = startTime.AddHours(i);
                var nextHour = hour.AddHours(1);
                var opensInHour = recipients.Count(r =>
                    r.OpenedAt.HasValue &&
                    r.OpenedAt.Value >= hour &&
                    r.OpenedAt.Value < nextHour);

                openTimeline.Add(new TimeSeriesPoint(hour, opensInHour));
            }
        }

        // Device breakdown (simplified - in production, parse User-Agent properly)
        var deviceBreakdown = recipients
            .SelectMany(r => r.Clicks)
            .Where(c => !string.IsNullOrEmpty(c.UserAgent))
            .GroupBy(c => DetectDevice(c.UserAgent))
            .Select(g => new DeviceStats(
                g.Key,
                g.Count(),
                clicked > 0 ? (decimal)g.Count() / clicked * 100 : 0))
            .OrderByDescending(d => d.Count)
            .ToList();

        return new NewsletterAnalytics
        {
            TotalRecipients = totalRecipients,
            Sent = sent,
            Delivered = delivered,
            Opened = opened,
            Clicked = clicked,
            Bounced = bounced,
            OpenRate = Math.Round(openRate, 2),
            ClickRate = Math.Round(clickRate, 2),
            ClickToOpenRate = Math.Round(clickToOpenRate, 2),
            TopLinks = topLinks,
            OpenTimeline = openTimeline,
            DeviceBreakdown = deviceBreakdown
        };
    }

    private async Task<List<EmailRecipient>> ResolveRecipientsAsync(
        EmailNewsletter newsletter,
        CancellationToken cancellationToken)
    {
        IQueryable<User> usersQuery = _context.Users.Where(u => !u.IsDeleted && u.IsActive);

        switch (newsletter.DistributionType)
        {
            case DistributionType.AllUsers:
                break;

            case DistributionType.UserGroups:
                if (!string.IsNullOrEmpty(newsletter.TargetGroupIds))
                {
                    var groupIds = JsonSerializer.Deserialize<List<Guid>>(newsletter.TargetGroupIds);
                    if (groupIds?.Any() == true)
                    {
                        usersQuery = usersQuery.Where(u =>
                            u.GroupMemberships.Any(gm => groupIds.Contains(gm.UserGroupId)));
                    }
                }
                break;

            case DistributionType.CustomList:
                // For custom list, the target group IDs field contains user IDs directly
                if (!string.IsNullOrEmpty(newsletter.TargetGroupIds))
                {
                    var userIds = JsonSerializer.Deserialize<List<Guid>>(newsletter.TargetGroupIds);
                    if (userIds?.Any() == true)
                    {
                        usersQuery = usersQuery.Where(u => userIds.Contains(u.Id));
                    }
                }
                break;
        }

        var users = await usersQuery
            .Where(u => !string.IsNullOrEmpty(u.Email))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(cancellationToken);

        return users.Select(u => new EmailRecipient
        {
            NewsletterId = newsletter.Id,
            UserId = u.Id,
            Email = u.Email!,
            TrackingToken = GenerateTrackingToken(),
            Status = EmailRecipientStatus.Pending
        }).ToList();
    }

    private static string GenerateTrackingToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string DetectDevice(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        var ua = userAgent.ToLowerInvariant();

        if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone"))
            return "Mobile";
        if (ua.Contains("tablet") || ua.Contains("ipad"))
            return "Tablet";
        if (ua.Contains("windows") || ua.Contains("macintosh") || ua.Contains("linux"))
            return "Desktop";

        return "Other";
    }
}

public record NewsletterSendJob(Guid NewsletterId);
