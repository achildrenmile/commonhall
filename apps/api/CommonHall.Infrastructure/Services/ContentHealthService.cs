using System.Text.RegularExpressions;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Infrastructure.Persistence;
using CommonHall.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.Services;

public sealed class ContentHealthService : IContentHealthService
{
    private readonly CommonHallDbContext _dbContext;
    private readonly IAiService _aiService;
    private readonly ILogger<ContentHealthService> _logger;
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromDays(180); // 6 months
    private static readonly int LowViewThreshold = 10;

    public ContentHealthService(
        CommonHallDbContext dbContext,
        IAiService aiService,
        ILogger<ContentHealthService> logger)
    {
        _dbContext = dbContext;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<Guid> StartScanAsync(CancellationToken cancellationToken = default)
    {
        var report = new ContentHealthReport
        {
            Id = Guid.NewGuid(),
            ScanStartedAt = DateTimeOffset.UtcNow,
            Status = "running"
        };

        _dbContext.ContentHealthReports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Run scan in background
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteScanAsync(report.Id, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Content health scan failed for report {ReportId}", report.Id);
                await MarkScanFailedAsync(report.Id);
            }
        }, CancellationToken.None);

        return report.Id;
    }

    private async Task ExecuteScanAsync(Guid reportId, CancellationToken cancellationToken)
    {
        var report = await _dbContext.ContentHealthReports.FindAsync([reportId], cancellationToken);
        if (report == null) return;

        var issues = new List<ContentHealthIssue>();
        var now = DateTimeOffset.UtcNow;
        var staleDate = now - StaleThreshold;

        // Check news articles
        var newsArticles = await _dbContext.NewsArticles
            .Where(a => !a.IsDeleted)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Slug,
                a.Content,
                a.UpdatedAt,
                a.PublishedAt,
                a.ViewCount
            })
            .ToListAsync(cancellationToken);

        report.TotalContentCount += newsArticles.Count;

        foreach (var article in newsArticles)
        {
            var url = $"/news/{article.Slug}";

            // Check for stale content (not updated in 6 months)
            if (article.UpdatedAt < staleDate)
            {
                var daysSinceUpdate = (now - article.UpdatedAt).Days;
                issues.Add(new ContentHealthIssue
                {
                    Id = Guid.NewGuid(),
                    ReportId = reportId,
                    ContentType = "news",
                    ContentId = article.Id,
                    ContentTitle = article.Title,
                    ContentUrl = url,
                    IssueType = "stale",
                    Severity = daysSinceUpdate > 365 ? "high" : "medium",
                    Description = $"Article hasn't been updated in {daysSinceUpdate} days",
                    Recommendation = "Review and update this content or archive if no longer relevant"
                });
                report.StaleContentCount++;
            }

            // Check for low engagement
            if (article.ViewCount < LowViewThreshold && article.PublishedAt < now.AddDays(-30))
            {
                issues.Add(new ContentHealthIssue
                {
                    Id = Guid.NewGuid(),
                    ReportId = reportId,
                    ContentType = "news",
                    ContentId = article.Id,
                    ContentTitle = article.Title,
                    ContentUrl = url,
                    IssueType = "low_engagement",
                    Severity = "low",
                    Description = $"Only {article.ViewCount} views in 30+ days since publishing",
                    Recommendation = "Consider promoting this content or reviewing its relevance"
                });
                report.LowEngagementCount++;
            }

            // Check for broken links in content
            var contentText = ContentTextExtractor.ExtractText(article.Content);
            var brokenLinks = await CheckForBrokenLinksAsync(contentText, cancellationToken);
            foreach (var link in brokenLinks)
            {
                issues.Add(new ContentHealthIssue
                {
                    Id = Guid.NewGuid(),
                    ReportId = reportId,
                    ContentType = "news",
                    ContentId = article.Id,
                    ContentTitle = article.Title,
                    ContentUrl = url,
                    IssueType = "broken_link",
                    Severity = "high",
                    Description = $"Broken link found: {link}",
                    Recommendation = "Update or remove the broken link"
                });
                report.BrokenLinkCount++;
            }
        }

        // Check pages
        var pages = await _dbContext.Pages
            .Where(p => !p.IsDeleted)
            .Include(p => p.Space)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Content,
                p.UpdatedAt,
                SpaceSlug = p.Space != null ? p.Space.Slug : ""
            })
            .ToListAsync(cancellationToken);

        report.TotalContentCount += pages.Count;

        foreach (var page in pages)
        {
            var url = $"/spaces/{page.SpaceSlug}/{page.Slug}";

            // Check for stale content
            if (page.UpdatedAt < staleDate)
            {
                var daysSinceUpdate = (now - page.UpdatedAt).Days;
                issues.Add(new ContentHealthIssue
                {
                    Id = Guid.NewGuid(),
                    ReportId = reportId,
                    ContentType = "page",
                    ContentId = page.Id,
                    ContentTitle = page.Title,
                    ContentUrl = url,
                    IssueType = "stale",
                    Severity = daysSinceUpdate > 365 ? "high" : "medium",
                    Description = $"Page hasn't been updated in {daysSinceUpdate} days",
                    Recommendation = "Review and update this page or archive if no longer needed"
                });
                report.StaleContentCount++;
            }

            // Check for broken links
            var contentText = ContentTextExtractor.ExtractText(page.Content);
            var brokenLinks = await CheckForBrokenLinksAsync(contentText, cancellationToken);
            foreach (var link in brokenLinks)
            {
                issues.Add(new ContentHealthIssue
                {
                    Id = Guid.NewGuid(),
                    ReportId = reportId,
                    ContentType = "page",
                    ContentId = page.Id,
                    ContentTitle = page.Title,
                    ContentUrl = url,
                    IssueType = "broken_link",
                    Severity = "high",
                    Description = $"Broken link found: {link}",
                    Recommendation = "Update or remove the broken link"
                });
                report.BrokenLinkCount++;
            }
        }

        // Check for unused files (files not referenced anywhere)
        var filesWithNoReferences = await _dbContext.StoredFiles
            .Where(f => !f.IsDeleted && f.CollectionId == null)
            .Where(f => f.CreatedAt < now.AddDays(-30))
            .Select(f => new { f.Id, f.OriginalFileName, f.Url })
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var file in filesWithNoReferences)
        {
            // Check if file URL appears in any content
            var isReferenced = await _dbContext.NewsArticles
                .AnyAsync(a => a.Content != null && a.Content.Contains(file.Url), cancellationToken) ||
                await _dbContext.Pages
                .AnyAsync(p => p.Content != null && p.Content.Contains(file.Url), cancellationToken);

            if (!isReferenced)
            {
                issues.Add(new ContentHealthIssue
                {
                    Id = Guid.NewGuid(),
                    ReportId = reportId,
                    ContentType = "file",
                    ContentId = file.Id,
                    ContentTitle = file.OriginalFileName,
                    ContentUrl = file.Url,
                    IssueType = "unused",
                    Severity = "low",
                    Description = "File is not referenced by any content",
                    Recommendation = "Consider deleting unused files to save storage"
                });
                report.UnusedContentCount++;
            }
        }

        // Generate AI summary
        if (issues.Count > 0)
        {
            report.Summary = await GenerateSummaryAsync(issues, cancellationToken);
        }
        else
        {
            report.Summary = "No content health issues found. Your content is in great shape!";
        }

        // Save all issues
        foreach (var issue in issues)
        {
            _dbContext.ContentHealthIssues.Add(issue);
        }

        report.Status = "completed";
        report.ScanCompletedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Content health scan completed. Found {IssueCount} issues", issues.Count);
    }

    private async Task<List<string>> CheckForBrokenLinksAsync(string content, CancellationToken cancellationToken)
    {
        var brokenLinks = new List<string>();

        // Extract URLs from content
        var urlPattern = new Regex(@"https?://[^\s\]\)""<>]+", RegexOptions.IgnoreCase);
        var matches = urlPattern.Matches(content);

        foreach (Match match in matches.Take(10)) // Limit to prevent too many checks
        {
            var url = match.Value.TrimEnd('.', ',', '!', '?', ')');

            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                var response = await httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.MethodNotAllowed)
                {
                    brokenLinks.Add(url);
                }
            }
            catch
            {
                // URL is unreachable
                brokenLinks.Add(url);
            }
        }

        return brokenLinks;
    }

    private async Task<string> GenerateSummaryAsync(List<ContentHealthIssue> issues, CancellationToken cancellationToken)
    {
        var issueBreakdown = issues
            .GroupBy(i => i.IssueType)
            .Select(g => $"- {g.Key}: {g.Count()}")
            .ToList();

        var severityBreakdown = issues
            .GroupBy(i => i.Severity)
            .Select(g => $"- {g.Key}: {g.Count()}")
            .ToList();

        var prompt = $@"Generate a brief 2-3 sentence summary of these content health scan results:

Total issues: {issues.Count}

By type:
{string.Join("\n", issueBreakdown)}

By severity:
{string.Join("\n", severityBreakdown)}

Focus on the most critical findings and suggest one key action.";

        try
        {
            return await _aiService.GenerateAsync(
                "You are a content health analyst. Provide concise, actionable summaries.",
                prompt,
                new AiGenerationOptions { MaxTokens = 150, Temperature = 0.5 },
                cancellationToken);
        }
        catch
        {
            return $"Found {issues.Count} content health issues requiring attention.";
        }
    }

    private async Task MarkScanFailedAsync(Guid reportId)
    {
        var report = await _dbContext.ContentHealthReports.FindAsync(reportId);
        if (report != null)
        {
            report.Status = "failed";
            report.ScanCompletedAt = DateTimeOffset.UtcNow;
            report.Summary = "Scan failed due to an unexpected error";
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<ContentHealthReportDto?> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = await _dbContext.ContentHealthReports
            .Include(r => r.Issues)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        return report == null ? null : MapToDto(report);
    }

    public async Task<ContentHealthReportDto?> GetLatestReportAsync(CancellationToken cancellationToken = default)
    {
        var report = await _dbContext.ContentHealthReports
            .Include(r => r.Issues)
            .OrderByDescending(r => r.ScanStartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return report == null ? null : MapToDto(report);
    }

    public async Task<List<ContentHealthReportSummary>> GetReportHistoryAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ContentHealthReports
            .OrderByDescending(r => r.ScanStartedAt)
            .Take(limit)
            .Select(r => new ContentHealthReportSummary
            {
                Id = r.Id,
                ScanStartedAt = r.ScanStartedAt,
                ScanCompletedAt = r.ScanCompletedAt,
                Status = r.Status,
                TotalIssueCount = r.Issues.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task ResolveIssueAsync(Guid issueId, Guid resolvedBy, CancellationToken cancellationToken = default)
    {
        var issue = await _dbContext.ContentHealthIssues.FindAsync([issueId], cancellationToken);
        if (issue != null)
        {
            issue.IsResolved = true;
            issue.ResolvedAt = DateTimeOffset.UtcNow;
            issue.ResolvedBy = resolvedBy;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static ContentHealthReportDto MapToDto(ContentHealthReport report)
    {
        return new ContentHealthReportDto
        {
            Id = report.Id,
            ScanStartedAt = report.ScanStartedAt,
            ScanCompletedAt = report.ScanCompletedAt,
            Status = report.Status,
            TotalContentCount = report.TotalContentCount,
            StaleContentCount = report.StaleContentCount,
            BrokenLinkCount = report.BrokenLinkCount,
            UnusedContentCount = report.UnusedContentCount,
            LowEngagementCount = report.LowEngagementCount,
            Summary = report.Summary,
            Issues = report.Issues.Select(i => new ContentHealthIssueDto
            {
                Id = i.Id,
                ContentType = i.ContentType,
                ContentId = i.ContentId,
                ContentTitle = i.ContentTitle,
                ContentUrl = i.ContentUrl,
                IssueType = i.IssueType,
                Severity = i.Severity,
                Description = i.Description,
                Recommendation = i.Recommendation,
                IsResolved = i.IsResolved,
                ResolvedAt = i.ResolvedAt
            }).ToList()
        };
    }
}
