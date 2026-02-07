namespace CommonHall.Application.Interfaces;

public interface INotificationService
{
    Task SendAsync(
        Guid userId,
        string type,
        string title,
        string? message = null,
        string? actionUrl = null,
        CancellationToken cancellationToken = default);

    Task SendToManyAsync(
        IEnumerable<Guid> userIds,
        string type,
        string title,
        string? message = null,
        string? actionUrl = null,
        CancellationToken cancellationToken = default);
}
