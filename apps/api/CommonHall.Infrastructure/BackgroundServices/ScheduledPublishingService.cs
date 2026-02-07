using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CommonHall.Infrastructure.BackgroundServices;

public sealed class ScheduledPublishingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ScheduledPublishingService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(55);
    private const string LockKey = "scheduled-publishing:lock";

    public ScheduledPublishingService(
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer redis,
        ILogger<ScheduledPublishingService> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduledPublishingService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledArticlesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled articles");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        _logger.LogInformation("ScheduledPublishingService stopped");
    }

    private async Task ProcessScheduledArticlesAsync(CancellationToken cancellationToken)
    {
        var database = _redis.GetDatabase();
        var lockValue = Guid.NewGuid().ToString();

        // Try to acquire distributed lock
        var lockAcquired = await database.StringSetAsync(
            LockKey,
            lockValue,
            LockExpiry,
            When.NotExists);

        if (!lockAcquired)
        {
            _logger.LogDebug("Could not acquire lock for scheduled publishing, another instance is processing");
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var now = DateTimeOffset.UtcNow;

            var articlesToPublish = await context.NewsArticles
                .Where(a => a.Status == ContentStatus.Scheduled &&
                           a.ScheduledAt.HasValue &&
                           a.ScheduledAt.Value <= now)
                .ToListAsync(cancellationToken);

            if (articlesToPublish.Count == 0)
            {
                _logger.LogDebug("No scheduled articles to publish");
                return;
            }

            _logger.LogInformation("Publishing {Count} scheduled articles", articlesToPublish.Count);

            foreach (var article in articlesToPublish)
            {
                article.Status = ContentStatus.Published;
                article.PublishedAt = now;
                article.ScheduledAt = null;

                _logger.LogInformation("Published article {ArticleId}: {Title}", article.Id, article.Title);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            // Release lock only if we still own it
            var script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";

            await database.ScriptEvaluateAsync(script, [new RedisKey(LockKey)], [new RedisValue(lockValue)]);
        }
    }
}
