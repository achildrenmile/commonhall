using CommonHall.Domain.Entities;

namespace CommonHall.Application.Interfaces;

public interface ITagService
{
    Task<List<Tag>> SyncTagsAsync(Guid articleId, List<string> tagNames, CancellationToken cancellationToken = default);
    Task<List<Tag>> GetOrCreateTagsAsync(List<string> tagNames, CancellationToken cancellationToken = default);
}
