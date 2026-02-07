using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class ContentHealthReport : BaseEntity
{
    public DateTimeOffset ScanStartedAt { get; set; }
    public DateTimeOffset? ScanCompletedAt { get; set; }
    public string Status { get; set; } = "running"; // running, completed, failed
    public int TotalContentCount { get; set; }
    public int StaleContentCount { get; set; }
    public int BrokenLinkCount { get; set; }
    public int UnusedContentCount { get; set; }
    public int LowEngagementCount { get; set; }
    public string? Summary { get; set; }

    // Navigation
    public ICollection<ContentHealthIssue> Issues { get; set; } = new List<ContentHealthIssue>();
}

public sealed class ContentHealthIssue : BaseEntity
{
    public Guid ReportId { get; set; }
    public string ContentType { get; set; } = string.Empty; // news, page, file
    public Guid ContentId { get; set; }
    public string ContentTitle { get; set; } = string.Empty;
    public string ContentUrl { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty; // stale, broken_link, unused, low_engagement
    public string Severity { get; set; } = "medium"; // low, medium, high
    public string Description { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
    public bool IsResolved { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }

    // Navigation
    public ContentHealthReport Report { get; set; } = null!;
}
