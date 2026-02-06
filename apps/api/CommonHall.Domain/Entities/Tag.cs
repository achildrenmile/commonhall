using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}
