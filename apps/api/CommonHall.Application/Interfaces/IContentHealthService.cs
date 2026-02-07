namespace CommonHall.Application.Interfaces;

public interface IContentHealthService
{
    Task<Guid> StartScanAsync(CancellationToken cancellationToken = default);
    Task<ContentHealthReportDto?> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<ContentHealthReportDto?> GetLatestReportAsync(CancellationToken cancellationToken = default);
    Task<List<ContentHealthReportSummary>> GetReportHistoryAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task ResolveIssueAsync(Guid issueId, Guid resolvedBy, CancellationToken cancellationToken = default);
}

public record ContentHealthReportDto
{
    public Guid Id { get; init; }
    public DateTimeOffset ScanStartedAt { get; init; }
    public DateTimeOffset? ScanCompletedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalContentCount { get; init; }
    public int StaleContentCount { get; init; }
    public int BrokenLinkCount { get; init; }
    public int UnusedContentCount { get; init; }
    public int LowEngagementCount { get; init; }
    public string? Summary { get; init; }
    public List<ContentHealthIssueDto> Issues { get; init; } = new();
}

public record ContentHealthIssueDto
{
    public Guid Id { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public Guid ContentId { get; init; }
    public string ContentTitle { get; init; } = string.Empty;
    public string ContentUrl { get; init; } = string.Empty;
    public string IssueType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Recommendation { get; init; }
    public bool IsResolved { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
}

public record ContentHealthReportSummary
{
    public Guid Id { get; init; }
    public DateTimeOffset ScanStartedAt { get; init; }
    public DateTimeOffset? ScanCompletedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalIssueCount { get; init; }
}
