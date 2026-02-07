using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Api.Controllers;

[Authorize]
public class NewslettersController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly INewsletterService _newsletterService;
    private readonly IEmailRenderer _emailRenderer;

    public NewslettersController(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        INewsletterService newsletterService,
        IEmailRenderer emailRenderer)
    {
        _context = context;
        _currentUser = currentUser;
        _newsletterService = newsletterService;
        _emailRenderer = emailRenderer;
    }

    /// <summary>
    /// Get all newsletters with optional filtering.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] NewsletterStatus? status,
        [FromQuery] int limit = 20,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var query = _context.EmailNewsletters
            .Where(n => !n.IsDeleted)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        // Cursor-based pagination
        if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId))
        {
            var cursorNewsletter = await _context.EmailNewsletters
                .FirstOrDefaultAsync(n => n.Id == cursorId, cancellationToken);

            if (cursorNewsletter != null)
            {
                query = query.Where(n => n.CreatedAt < cursorNewsletter.CreatedAt);
            }
        }

        var newsletters = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit + 1)
            .Select(n => new NewsletterListDto
            {
                Id = n.Id,
                Title = n.Title,
                Subject = n.Subject,
                Status = n.Status,
                DistributionType = n.DistributionType,
                ScheduledAt = n.ScheduledAt,
                SentAt = n.SentAt,
                CreatedAt = n.CreatedAt,
                RecipientCount = n.Recipients.Count,
                OpenCount = n.Recipients.Count(r => r.OpenedAt.HasValue),
                ClickCount = n.Recipients.Count(r => r.ClickedAt.HasValue)
            })
            .ToListAsync(cancellationToken);

        var hasMore = newsletters.Count > limit;
        if (hasMore)
            newsletters = newsletters.Take(limit).ToList();

        return Ok(new
        {
            items = newsletters,
            nextCursor = hasMore ? newsletters.Last().Id.ToString() : null,
            hasMore
        });
    }

    /// <summary>
    /// Get a single newsletter by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var newsletter = await _context.EmailNewsletters
            .Include(n => n.Template)
            .Where(n => n.Id == id && !n.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (newsletter == null)
            return NotFound();

        return Ok(new NewsletterDetailDto
        {
            Id = newsletter.Id,
            Title = newsletter.Title,
            Subject = newsletter.Subject,
            PreviewText = newsletter.PreviewText,
            Content = newsletter.Content,
            TemplateId = newsletter.TemplateId,
            TemplateName = newsletter.Template?.Name,
            Status = newsletter.Status,
            DistributionType = newsletter.DistributionType,
            TargetGroupIds = newsletter.TargetGroupIds,
            ScheduledAt = newsletter.ScheduledAt,
            SentAt = newsletter.SentAt,
            CreatedAt = newsletter.CreatedAt,
            UpdatedAt = newsletter.UpdatedAt
        });
    }

    /// <summary>
    /// Create a new newsletter.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateNewsletterRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var newsletter = new EmailNewsletter
        {
            Title = request.Title,
            Subject = request.Subject,
            PreviewText = request.PreviewText,
            Content = request.Content ?? "[]",
            TemplateId = request.TemplateId,
            DistributionType = request.DistributionType ?? DistributionType.AllUsers,
            TargetGroupIds = request.TargetGroupIds,
            Status = NewsletterStatus.Draft
        };

        _context.EmailNewsletters.Add(newsletter);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = newsletter.Id }, new NewsletterDetailDto
        {
            Id = newsletter.Id,
            Title = newsletter.Title,
            Subject = newsletter.Subject,
            PreviewText = newsletter.PreviewText,
            Content = newsletter.Content,
            TemplateId = newsletter.TemplateId,
            Status = newsletter.Status,
            DistributionType = newsletter.DistributionType,
            TargetGroupIds = newsletter.TargetGroupIds,
            CreatedAt = newsletter.CreatedAt,
            UpdatedAt = newsletter.UpdatedAt
        });
    }

    /// <summary>
    /// Update a newsletter.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateNewsletterRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var newsletter = await _context.EmailNewsletters
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken);

        if (newsletter == null)
            return NotFound();

        if (newsletter.Status != NewsletterStatus.Draft)
            return BadRequest(new { error = "Only draft newsletters can be modified" });

        if (request.Title != null)
            newsletter.Title = request.Title;
        if (request.Subject != null)
            newsletter.Subject = request.Subject;
        if (request.PreviewText != null)
            newsletter.PreviewText = request.PreviewText;
        if (request.Content != null)
            newsletter.Content = request.Content;
        if (request.TemplateId.HasValue)
            newsletter.TemplateId = request.TemplateId.Value == Guid.Empty ? null : request.TemplateId.Value;
        if (request.DistributionType.HasValue)
            newsletter.DistributionType = request.DistributionType.Value;
        if (request.TargetGroupIds != null)
            newsletter.TargetGroupIds = request.TargetGroupIds;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new NewsletterDetailDto
        {
            Id = newsletter.Id,
            Title = newsletter.Title,
            Subject = newsletter.Subject,
            PreviewText = newsletter.PreviewText,
            Content = newsletter.Content,
            TemplateId = newsletter.TemplateId,
            Status = newsletter.Status,
            DistributionType = newsletter.DistributionType,
            TargetGroupIds = newsletter.TargetGroupIds,
            CreatedAt = newsletter.CreatedAt,
            UpdatedAt = newsletter.UpdatedAt
        });
    }

    /// <summary>
    /// Delete a newsletter.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var newsletter = await _context.EmailNewsletters
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken);

        if (newsletter == null)
            return NotFound();

        if (newsletter.Status == NewsletterStatus.Sending)
            return BadRequest(new { error = "Cannot delete a newsletter that is currently sending" });

        newsletter.IsDeleted = true;
        newsletter.DeletedAt = DateTimeOffset.UtcNow;
        newsletter.DeletedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Send a newsletter immediately.
    /// </summary>
    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var success = await _newsletterService.QueueForSendingAsync(id, cancellationToken);

        if (!success)
            return BadRequest(new { error = "Failed to queue newsletter for sending" });

        return Ok(new { message = "Newsletter queued for sending" });
    }

    /// <summary>
    /// Schedule a newsletter for future sending.
    /// </summary>
    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> Schedule(
        Guid id,
        [FromBody] ScheduleNewsletterRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var success = await _newsletterService.ScheduleAsync(id, request.ScheduledAt, cancellationToken);

        if (!success)
            return BadRequest(new { error = "Failed to schedule newsletter" });

        return Ok(new { message = "Newsletter scheduled", scheduledAt = request.ScheduledAt });
    }

    /// <summary>
    /// Send a test email to verify the newsletter.
    /// </summary>
    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> SendTest(
        Guid id,
        [FromBody] SendTestRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var success = await _newsletterService.SendTestAsync(id, request.Email, cancellationToken);

        if (!success)
            return BadRequest(new { error = "Failed to send test email" });

        return Ok(new { message = $"Test email sent to {request.Email}" });
    }

    /// <summary>
    /// Get newsletter analytics.
    /// </summary>
    [HttpGet("{id:guid}/analytics")]
    public async Task<IActionResult> GetAnalytics(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var newsletter = await _context.EmailNewsletters
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken);

        if (newsletter == null)
            return NotFound();

        var analytics = await _newsletterService.GetAnalyticsAsync(id, cancellationToken);
        return Ok(analytics);
    }

    /// <summary>
    /// Get HTML preview of the newsletter.
    /// </summary>
    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> GetPreview(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var newsletter = await _context.EmailNewsletters
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken);

        if (newsletter == null)
            return NotFound();

        var html = await _emailRenderer.RenderPreviewAsync(newsletter, cancellationToken);
        return Content(html, "text/html");
    }
}

public record CreateNewsletterRequest
{
    public required string Title { get; init; }
    public required string Subject { get; init; }
    public string? PreviewText { get; init; }
    public string? Content { get; init; }
    public Guid? TemplateId { get; init; }
    public DistributionType? DistributionType { get; init; }
    public string? TargetGroupIds { get; init; }
}

public record UpdateNewsletterRequest
{
    public string? Title { get; init; }
    public string? Subject { get; init; }
    public string? PreviewText { get; init; }
    public string? Content { get; init; }
    public Guid? TemplateId { get; init; }
    public DistributionType? DistributionType { get; init; }
    public string? TargetGroupIds { get; init; }
}

public record ScheduleNewsletterRequest
{
    public DateTimeOffset ScheduledAt { get; init; }
}

public record SendTestRequest
{
    public required string Email { get; init; }
}

public record NewsletterListDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Subject { get; init; }
    public NewsletterStatus Status { get; init; }
    public DistributionType DistributionType { get; init; }
    public DateTimeOffset? ScheduledAt { get; init; }
    public DateTimeOffset? SentAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int RecipientCount { get; init; }
    public int OpenCount { get; init; }
    public int ClickCount { get; init; }
    public decimal OpenRate => RecipientCount > 0 ? Math.Round((decimal)OpenCount / RecipientCount * 100, 1) : 0;
    public decimal ClickRate => RecipientCount > 0 ? Math.Round((decimal)ClickCount / RecipientCount * 100, 1) : 0;
}

public record NewsletterDetailDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Subject { get; init; }
    public string? PreviewText { get; init; }
    public required string Content { get; init; }
    public Guid? TemplateId { get; init; }
    public string? TemplateName { get; init; }
    public NewsletterStatus Status { get; init; }
    public DistributionType DistributionType { get; init; }
    public string? TargetGroupIds { get; init; }
    public DateTimeOffset? ScheduledAt { get; init; }
    public DateTimeOffset? SentAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
