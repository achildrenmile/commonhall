using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CommonHall.Infrastructure.BackgroundServices;

public sealed class JourneyProgressionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<JourneyProgressionService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(5);
    private const string LockKey = "journey:progression:lock";

    public JourneyProgressionService(
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer? redis,
        ILogger<JourneyProgressionService> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Journey progression service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
                await ProcessEnrollmentsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in journey progression service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Journey progression service stopped");
    }

    private async Task ProcessEnrollmentsAsync(CancellationToken cancellationToken)
    {
        // Try to acquire distributed lock
        if (!await TryAcquireLockAsync())
        {
            _logger.LogDebug("Another instance is processing journey progression");
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var journeyService = scope.ServiceProvider.GetRequiredService<IJourneyService>();

            var now = DateTimeOffset.UtcNow;

            // Get active enrollments that may need progression
            var enrollments = await context.JourneyEnrollments
                .Include(e => e.Journey)
                .ThenInclude(j => j.Steps)
                .Include(e => e.StepCompletions)
                .Where(e => e.Status == JourneyEnrollmentStatus.Active)
                .Where(e => e.Journey.IsActive && !e.Journey.IsDeleted)
                .ToListAsync(cancellationToken);

            var processedCount = 0;

            foreach (var enrollment in enrollments)
            {
                try
                {
                    var steps = enrollment.Journey.Steps.OrderBy(s => s.SortOrder).ToList();

                    // Check if all steps completed
                    if (enrollment.CurrentStepIndex >= steps.Count)
                    {
                        enrollment.Status = JourneyEnrollmentStatus.Completed;
                        enrollment.CompletedAt = now;
                        await context.SaveChangesAsync(cancellationToken);
                        continue;
                    }

                    var currentStep = steps[enrollment.CurrentStepIndex];

                    // Check if current step was already delivered
                    var stepDelivered = enrollment.StepCompletions
                        .Any(c => c.StepIndex == enrollment.CurrentStepIndex);

                    if (!stepDelivered)
                    {
                        // Check if delay has elapsed since start or last step delivery
                        var referenceTime = enrollment.LastStepDeliveredAt ?? enrollment.StartedAt;
                        var delayElapsed = now >= referenceTime.AddDays(currentStep.DelayDays);

                        if (delayElapsed)
                        {
                            await journeyService.DeliverNextStepAsync(enrollment, cancellationToken);
                            processedCount++;
                        }
                    }
                    else
                    {
                        // Step was delivered, check if required step is completed
                        var completion = enrollment.StepCompletions
                            .First(c => c.StepIndex == enrollment.CurrentStepIndex);

                        if (!currentStep.IsRequired || completion.CompletedAt.HasValue)
                        {
                            // Auto-advance if not required or already completed
                            if (!completion.CompletedAt.HasValue && !currentStep.IsRequired)
                            {
                                // Auto-complete non-required steps after 7 days
                                var autoCompleteAfter = completion.DeliveredAt.AddDays(7);
                                if (now >= autoCompleteAfter)
                                {
                                    completion.CompletedAt = now;
                                    enrollment.CurrentStepIndex++;
                                    await context.SaveChangesAsync(cancellationToken);
                                    processedCount++;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing enrollment {EnrollmentId}", enrollment.Id);
                }
            }

            if (processedCount > 0)
            {
                _logger.LogInformation("Processed {Count} journey step deliveries", processedCount);
            }
        }
        finally
        {
            await ReleaseLockAsync();
        }
    }

    private async Task<bool> TryAcquireLockAsync()
    {
        if (_redis == null)
            return true; // No Redis, assume single instance

        try
        {
            var db = _redis.GetDatabase();
            return await db.StringSetAsync(
                LockKey,
                Environment.MachineName,
                LockTimeout,
                When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to acquire Redis lock, proceeding anyway");
            return true;
        }
    }

    private async Task ReleaseLockAsync()
    {
        if (_redis == null)
            return;

        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(LockKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to release Redis lock");
        }
    }
}
