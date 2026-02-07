using CommonHall.Domain.Entities;
using CommonHall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.Services;

public sealed class TrackingEventProcessor : BackgroundService
{
    private readonly TrackingEventChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrackingEventProcessor> _logger;
    private readonly List<TrackingEvent> _buffer = new();
    private readonly object _bufferLock = new();
    private const int BatchSize = 100;
    private const int FlushIntervalSeconds = 5;

    public TrackingEventProcessor(
        TrackingEventChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<TrackingEventProcessor> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TrackingEventProcessor started");

        var flushTimer = new PeriodicTimer(TimeSpan.FromSeconds(FlushIntervalSeconds));
        var flushTask = FlushPeriodicallyAsync(flushTimer, stoppingToken);

        try
        {
            await foreach (var trackingEvent in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                bool shouldFlush = false;

                lock (_bufferLock)
                {
                    _buffer.Add(trackingEvent);
                    shouldFlush = _buffer.Count >= BatchSize;
                }

                if (shouldFlush)
                {
                    await FlushBufferAsync(stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        finally
        {
            flushTimer.Dispose();
            await flushTask;

            // Final flush on shutdown
            await FlushBufferAsync(CancellationToken.None);
            _logger.LogInformation("TrackingEventProcessor stopped");
        }
    }

    private async Task FlushPeriodicallyAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushBufferAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
    }

    private async Task FlushBufferAsync(CancellationToken cancellationToken)
    {
        List<TrackingEvent> eventsToFlush;

        lock (_bufferLock)
        {
            if (_buffer.Count == 0)
                return;

            eventsToFlush = new List<TrackingEvent>(_buffer);
            _buffer.Clear();
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CommonHallDbContext>();

            await dbContext.TrackingEvents.AddRangeAsync(eventsToFlush, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Flushed {Count} tracking events to database", eventsToFlush.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush {Count} tracking events", eventsToFlush.Count);

            // Re-add events to buffer for retry (up to a limit to prevent memory issues)
            lock (_bufferLock)
            {
                if (_buffer.Count < 5000)
                {
                    _buffer.AddRange(eventsToFlush);
                }
            }
        }
    }
}
