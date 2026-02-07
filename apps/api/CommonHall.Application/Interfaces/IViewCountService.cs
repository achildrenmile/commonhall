namespace CommonHall.Application.Interfaces;

public interface IViewCountService
{
    Task<bool> TryIncrementViewCountAsync(Guid articleId, Guid? userId, CancellationToken cancellationToken = default);
}
