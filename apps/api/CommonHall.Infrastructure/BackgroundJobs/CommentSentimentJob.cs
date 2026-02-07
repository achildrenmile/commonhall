using CommonHall.Application.Interfaces;
using CommonHall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.BackgroundJobs;

public sealed class CommentSentimentJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommentSentimentJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    private readonly string[] _sentimentCategories = ["positive", "neutral", "negative"];
    private const int BatchSize = 50;

    public CommentSentimentJob(
        IServiceProvider serviceProvider,
        ILogger<CommentSentimentJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Comment Sentiment Job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingCommentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing comment sentiments");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessPendingCommentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommonHallDbContext>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();

        // Find comments without sentiment analysis
        var pendingComments = await dbContext.Comments
            .Where(c => !c.IsDeleted && c.SentimentLabel == null)
            .OrderBy(c => c.CreatedAt)
            .Take(BatchSize)
            .Select(c => new { c.Id, c.Body })
            .ToListAsync(cancellationToken);

        if (pendingComments.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} comments for sentiment analysis", pendingComments.Count);

        // Process in smaller batches for API efficiency
        const int apiBatchSize = 10;
        var batches = pendingComments.Chunk(apiBatchSize);

        foreach (var batch in batches)
        {
            try
            {
                var texts = batch.Select(c => c.Body).ToArray();
                var results = await aiService.ClassifyBatchAsync(
                    texts,
                    _sentimentCategories,
                    cancellationToken);

                // Update comments with results
                for (int i = 0; i < batch.Length && i < results.Count; i++)
                {
                    var comment = await dbContext.Comments.FindAsync([batch[i].Id], cancellationToken);
                    if (comment != null)
                    {
                        comment.SentimentLabel = results[i].Category.ToLower();
                        comment.SentimentScore = (decimal)results[i].Confidence;
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Updated sentiment for {Count} comments", batch.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to classify batch of comments");
            }
        }

        _logger.LogInformation("Completed sentiment analysis for {Count} comments", pendingComments.Count);
    }
}
