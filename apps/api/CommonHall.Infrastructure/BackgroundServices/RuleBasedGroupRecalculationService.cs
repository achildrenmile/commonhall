using CommonHall.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that recalculates rule-based group memberships every 15 minutes.
/// </summary>
public sealed class RuleBasedGroupRecalculationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RuleBasedGroupRecalculationService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    public RuleBasedGroupRecalculationService(
        IServiceProvider serviceProvider,
        ILogger<RuleBasedGroupRecalculationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Rule-based group recalculation service started. Interval: {Interval}", Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RecalculateGroupsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during rule-based group recalculation");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        _logger.LogInformation("Rule-based group recalculation service stopped");
    }

    private async Task RecalculateGroupsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var ruleBasedGroupService = scope.ServiceProvider.GetRequiredService<IRuleBasedGroupService>();

        _logger.LogDebug("Starting rule-based group recalculation");

        var results = await ruleBasedGroupService.RecalculateAllAsync(cancellationToken);

        var successCount = results.Count(r => r.Value >= 0);
        var errorCount = results.Count(r => r.Value < 0);

        _logger.LogInformation(
            "Rule-based group recalculation completed. Groups processed: {Total}, Success: {Success}, Errors: {Errors}",
            results.Count, successCount, errorCount);
    }
}
