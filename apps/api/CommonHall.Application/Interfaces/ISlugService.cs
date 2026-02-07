namespace CommonHall.Application.Interfaces;

public interface ISlugService
{
    string GenerateSlug(string title);
    Task<string> GenerateUniqueSpaceSlugAsync(string title, CancellationToken cancellationToken = default);
    Task<string> GenerateUniquePageSlugAsync(Guid spaceId, string title, CancellationToken cancellationToken = default);
}
