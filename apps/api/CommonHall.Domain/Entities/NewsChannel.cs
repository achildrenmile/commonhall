using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class NewsChannel : BaseEntity
{
    public Guid SpaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }

    // Navigation properties
    public Space Space { get; set; } = null!;
    public ICollection<NewsArticle> Articles { get; set; } = new List<NewsArticle>();
}
