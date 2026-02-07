using CommonHall.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.Services;

public sealed class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        Guid userId,
        string type,
        string title,
        string? message = null,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement real notification delivery via SignalR hub
        // For now, just log
        _logger.LogInformation(
            "Notification to {UserId}: [{Type}] {Title} - {Message}",
            userId, type, title, message);

        return Task.CompletedTask;
    }

    public Task SendToManyAsync(
        IEnumerable<Guid> userIds,
        string type,
        string title,
        string? message = null,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var userId in userIds)
        {
            _ = SendAsync(userId, type, title, message, actionUrl, cancellationToken);
        }

        return Task.CompletedTask;
    }
}
