using System.Collections.Concurrent;

namespace CommonHall.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, RateLimitState> _rateLimiters = new();
    private readonly Timer _cleanupTimer;

    private static readonly Dictionary<string, RateLimitConfig> PathConfigs = new()
    {
        { "/api/v1/ai/companion", new RateLimitConfig(20, TimeSpan.FromMinutes(1)) },
        { "/api/v1/ai/ask", new RateLimitConfig(10, TimeSpan.FromMinutes(1)) }
    };

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        // Clean up expired entries every 5 minutes
        _cleanupTimer = new Timer(_ => CleanupExpiredEntries(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Find matching rate limit config
        RateLimitConfig? config = null;
        foreach (var (pathPrefix, limitConfig) in PathConfigs)
        {
            if (path.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                config = limitConfig;
                break;
            }
        }

        if (config == null)
        {
            await _next(context);
            return;
        }

        // Get user identifier (user ID or IP address)
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var clientKey = $"{userId ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}:{path}";

        var state = _rateLimiters.GetOrAdd(clientKey, _ => new RateLimitState(config.Limit, config.Window));

        if (!state.TryConsume())
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientKey}", clientKey);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = state.GetRetryAfterSeconds().ToString();
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                retryAfterSeconds = state.GetRetryAfterSeconds()
            });
            return;
        }

        // Add rate limit headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-RateLimit-Limit"] = config.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = state.GetRemaining().ToString();
            context.Response.Headers["X-RateLimit-Reset"] = state.GetResetTime().ToUnixTimeSeconds().ToString();
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private void CleanupExpiredEntries()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _rateLimiters
            .Where(kvp => kvp.Value.IsExpired(now))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _rateLimiters.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired rate limit entries", expiredKeys.Count);
        }
    }
}

public record RateLimitConfig(int Limit, TimeSpan Window);

public class RateLimitState
{
    private readonly object _lock = new();
    private readonly int _limit;
    private readonly TimeSpan _window;
    private readonly Queue<DateTimeOffset> _requests = new();
    private DateTimeOffset _windowStart;

    public RateLimitState(int limit, TimeSpan window)
    {
        _limit = limit;
        _window = window;
        _windowStart = DateTimeOffset.UtcNow;
    }

    public bool TryConsume()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            CleanupOldRequests(now);

            if (_requests.Count >= _limit)
            {
                return false;
            }

            _requests.Enqueue(now);
            return true;
        }
    }

    public int GetRemaining()
    {
        lock (_lock)
        {
            CleanupOldRequests(DateTimeOffset.UtcNow);
            return Math.Max(0, _limit - _requests.Count);
        }
    }

    public int GetRetryAfterSeconds()
    {
        lock (_lock)
        {
            if (_requests.Count == 0)
                return 0;

            var oldest = _requests.Peek();
            var resetTime = oldest.Add(_window);
            return Math.Max(0, (int)(resetTime - DateTimeOffset.UtcNow).TotalSeconds);
        }
    }

    public DateTimeOffset GetResetTime()
    {
        lock (_lock)
        {
            if (_requests.Count == 0)
                return DateTimeOffset.UtcNow;

            var oldest = _requests.Peek();
            return oldest.Add(_window);
        }
    }

    public bool IsExpired(DateTimeOffset now)
    {
        lock (_lock)
        {
            CleanupOldRequests(now);
            return _requests.Count == 0 && (now - _windowStart) > _window * 2;
        }
    }

    private void CleanupOldRequests(DateTimeOffset now)
    {
        var cutoff = now - _window;
        while (_requests.Count > 0 && _requests.Peek() < cutoff)
        {
            _requests.Dequeue();
        }

        if (_requests.Count == 0)
        {
            _windowStart = now;
        }
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
