using System.Threading.Channels;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using CommonHall.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes the email sending queue.
/// </summary>
public sealed class EmailSendingBackgroundService : BackgroundService
{
    private readonly Channel<NewsletterSendJob> _sendQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailSendingBackgroundService> _logger;
    private readonly string _baseUrl;

    private const int BatchSize = 100;
    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays = {
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2)
    };

    public EmailSendingBackgroundService(
        Channel<NewsletterSendJob> sendQueue,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<EmailSendingBackgroundService> logger)
    {
        _sendQueue = sendQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _baseUrl = configuration["App:BaseUrl"] ?? "http://localhost:5000";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email sending background service started");

        await foreach (var job in _sendQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing newsletter send job {Id}", job.NewsletterId);
            }
        }
    }

    private async Task ProcessJobAsync(NewsletterSendJob job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing newsletter send job {Id}", job.NewsletterId);

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var emailRenderer = scope.ServiceProvider.GetRequiredService<IEmailRenderer>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailSendingService>();

        var newsletter = await context.EmailNewsletters
            .FirstOrDefaultAsync(n => n.Id == job.NewsletterId, cancellationToken);

        if (newsletter == null)
        {
            _logger.LogWarning("Newsletter {Id} not found", job.NewsletterId);
            return;
        }

        try
        {
            // Get pending recipients in batches
            var pendingRecipients = await context.EmailRecipients
                .Where(r => r.NewsletterId == job.NewsletterId && r.Status == EmailRecipientStatus.Pending)
                .ToListAsync(cancellationToken);

            var totalBatches = (int)Math.Ceiling((double)pendingRecipients.Count / BatchSize);
            var processedCount = 0;
            var successCount = 0;
            var failedCount = 0;

            for (var batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var batch = pendingRecipients
                    .Skip(batchIndex * BatchSize)
                    .Take(BatchSize)
                    .ToList();

                _logger.LogDebug("Processing batch {Batch}/{Total} for newsletter {Id}",
                    batchIndex + 1, totalBatches, job.NewsletterId);

                // Render and prepare messages
                var messages = new List<EmailMessage>();
                foreach (var recipient in batch)
                {
                    var html = await emailRenderer.RenderToHtmlAsync(
                        newsletter, recipient, _baseUrl, cancellationToken);

                    messages.Add(new EmailMessage(
                        recipient.Email,
                        newsletter.Subject,
                        html,
                        RecipientId: recipient.Id.ToString()));
                }

                // Send batch with retries
                var results = await SendWithRetriesAsync(
                    emailService, messages, cancellationToken);

                // Update recipient statuses
                foreach (var result in results)
                {
                    if (Guid.TryParse(result.RecipientId, out var recipientId))
                    {
                        var recipient = batch.FirstOrDefault(r => r.Id == recipientId);
                        if (recipient != null)
                        {
                            if (result.Success)
                            {
                                recipient.Status = EmailRecipientStatus.Sent;
                                recipient.SentAt = DateTimeOffset.UtcNow;
                                successCount++;
                            }
                            else
                            {
                                recipient.Status = EmailRecipientStatus.Failed;
                                recipient.ErrorMessage = result.ErrorMessage;
                                failedCount++;
                            }
                        }
                    }
                    processedCount++;
                }

                await context.SaveChangesAsync(cancellationToken);

                // Small delay between batches to prevent overwhelming
                if (batchIndex < totalBatches - 1)
                {
                    await Task.Delay(500, cancellationToken);
                }
            }

            // Update newsletter status
            newsletter.Status = failedCount == pendingRecipients.Count
                ? NewsletterStatus.Failed
                : NewsletterStatus.Sent;
            newsletter.SentAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Newsletter {Id} sending completed. Sent: {Success}, Failed: {Failed}",
                job.NewsletterId, successCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send newsletter {Id}", job.NewsletterId);

            newsletter.Status = NewsletterStatus.Failed;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<IEnumerable<EmailSendResult>> SendWithRetriesAsync(
        IEmailSendingService emailService,
        List<EmailMessage> messages,
        CancellationToken cancellationToken)
    {
        var results = new List<EmailSendResult>();
        var remainingMessages = messages.ToList();

        for (var attempt = 0; attempt <= MaxRetries && remainingMessages.Any(); attempt++)
        {
            if (attempt > 0)
            {
                var delay = RetryDelays[Math.Min(attempt - 1, RetryDelays.Length - 1)];
                _logger.LogDebug("Retrying {Count} failed emails after {Delay}",
                    remainingMessages.Count, delay);
                await Task.Delay(delay, cancellationToken);
            }

            var batchResults = await emailService.SendBulkAsync(remainingMessages, cancellationToken);
            var resultList = batchResults.ToList();

            // Separate successes and failures
            var successes = resultList.Where(r => r.Success).ToList();
            var failures = resultList.Where(r => !r.Success).ToList();

            results.AddRange(successes);

            if (attempt == MaxRetries)
            {
                // Final attempt - add remaining failures
                results.AddRange(failures);
            }
            else
            {
                // Prepare for retry
                remainingMessages = remainingMessages
                    .Where(m => failures.Any(f => f.RecipientId == m.RecipientId))
                    .ToList();
            }
        }

        return results;
    }
}
