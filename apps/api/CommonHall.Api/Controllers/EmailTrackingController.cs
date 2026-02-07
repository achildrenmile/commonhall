using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Api.Controllers;

/// <summary>
/// Handles email tracking for opens and clicks. No authentication required.
/// </summary>
[ApiController]
[Route("api/v1/email/track")]
public class EmailTrackingController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<EmailTrackingController> _logger;

    // 1x1 transparent GIF
    private static readonly byte[] TransparentGif = Convert.FromBase64String(
        "R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

    public EmailTrackingController(
        IApplicationDbContext context,
        ILogger<EmailTrackingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Records an email open. Returns a 1x1 transparent GIF.
    /// </summary>
    [HttpGet("{token}/open")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> TrackOpen(string token)
    {
        // Fire and forget DB update
        _ = Task.Run(async () =>
        {
            try
            {
                await RecordOpenAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record email open for token {Token}", token);
            }
        });

        return File(TransparentGif, "image/gif");
    }

    /// <summary>
    /// Records a link click and redirects to the target URL.
    /// </summary>
    [HttpGet("{token}/click")]
    public async Task<IActionResult> TrackClick(string token, [FromQuery] string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest("URL is required");
        }

        // Decode the URL
        var decodedUrl = Uri.UnescapeDataString(url);

        // Validate URL to prevent open redirect attacks
        if (!Uri.TryCreate(decodedUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            return BadRequest("Invalid URL");
        }

        // Fire and forget DB update
        _ = Task.Run(async () =>
        {
            try
            {
                await RecordClickAsync(token, decodedUrl, GetUserAgent(), GetIpAddress());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record email click for token {Token}", token);
            }
        });

        return Redirect(decodedUrl);
    }

    private async Task RecordOpenAsync(string token)
    {
        var recipient = await _context.EmailRecipients
            .FirstOrDefaultAsync(r => r.TrackingToken == token);

        if (recipient == null)
            return;

        recipient.OpenCount++;

        if (!recipient.OpenedAt.HasValue)
        {
            recipient.OpenedAt = DateTimeOffset.UtcNow;
        }

        if (recipient.Status == EmailRecipientStatus.Sent)
        {
            recipient.Status = EmailRecipientStatus.Delivered;
            recipient.DeliveredAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private async Task RecordClickAsync(string token, string url, string? userAgent, string? ipAddress)
    {
        var recipient = await _context.EmailRecipients
            .FirstOrDefaultAsync(r => r.TrackingToken == token);

        if (recipient == null)
            return;

        // Record the click
        var click = new EmailClick
        {
            RecipientId = recipient.Id,
            Url = url,
            ClickedAt = DateTimeOffset.UtcNow,
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
            IpAddress = ipAddress
        };

        _context.EmailClicks.Add(click);

        // Update recipient clicked timestamp
        if (!recipient.ClickedAt.HasValue)
        {
            recipient.ClickedAt = DateTimeOffset.UtcNow;
        }

        // If they clicked, they must have opened
        if (!recipient.OpenedAt.HasValue)
        {
            recipient.OpenedAt = DateTimeOffset.UtcNow;
            recipient.OpenCount = 1;
        }

        if (recipient.Status == EmailRecipientStatus.Sent)
        {
            recipient.Status = EmailRecipientStatus.Delivered;
            recipient.DeliveredAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private string? GetUserAgent()
    {
        return Request.Headers.UserAgent.FirstOrDefault();
    }

    private string? GetIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').FirstOrDefault()?.Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
