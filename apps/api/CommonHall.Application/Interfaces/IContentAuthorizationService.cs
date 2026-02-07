namespace CommonHall.Application.Interfaces;

public interface IContentAuthorizationService
{
    Task<bool> CanManageSpaceAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken = default);
    Task<bool> CanEditContentAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken = default);
    Task<bool> IsSpaceAdminAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken = default);
    Task<bool> IsPageCreatorAsync(Guid userId, Guid pageId, CancellationToken cancellationToken = default);
}
