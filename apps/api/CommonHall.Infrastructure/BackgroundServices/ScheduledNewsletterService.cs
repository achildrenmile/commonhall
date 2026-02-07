using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that checks for scheduled newsletters and triggers sending.
/// </summary>
public sealed class ScheduledNewsletterService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledNewsletterService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    public ScheduledNewsletterService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledNewsletterService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled newsletter service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledNewslettersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled newsletters");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task ProcessScheduledNewslettersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var newsletterService = scope.ServiceProvider.GetRequiredService<INewsletterService>();

        var now = DateTimeOffset.UtcNow;

        var scheduledNewsletters = await context.EmailNewsletters
            .Where(n => n.Status == NewsletterStatus.Scheduled &&
                        n.ScheduledAt.HasValue &&
                        n.ScheduledAt.Value <= now &&
                        !n.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var newsletter in scheduledNewsletters)
        {
            _logger.LogInformation("Triggering scheduled newsletter {Id}", newsletter.Id);

            try
            {
                await newsletterService.QueueForSendingAsync(newsletter.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger scheduled newsletter {Id}", newsletter.Id);
            }
        }
    }
}
