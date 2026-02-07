using CommonHall.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace CommonHall.Infrastructure.Services;

public sealed class ViewCountService : IViewCountService
{
    private readonly IApplicationDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan DeduplicationTtl = TimeSpan.FromMinutes(30);

    public ViewCountService(IApplicationDbContext context, IConnectionMultiplexer redis)
    {
        _context = context;
        _redis = redis;
    }

    public async Task<bool> TryIncrementViewCountAsync(Guid articleId, Guid? userId, CancellationToken cancellationToken = default)
    {
        var database = _redis.GetDatabase();

        // Generate deduplication key
        var viewerIdentifier = userId?.ToString() ?? "anonymous";
        var dedupKey = $"article-view:{articleId}:{viewerIdentifier}";

        // Check if this viewer has already viewed the article recently
        var alreadyViewed = await database.StringSetAsync(
            dedupKey,
            "1",
            DeduplicationTtl,
            When.NotExists);

        if (!alreadyViewed)
        {
            // Already viewed within TTL, don't increment
            return false;
        }

        // Increment view count atomically
        var article = await _context.NewsArticles
            .FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);

        if (article is null)
        {
            return false;
        }

        article.ViewCount++;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
