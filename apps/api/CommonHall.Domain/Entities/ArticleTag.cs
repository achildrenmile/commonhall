namespace CommonHall.Domain.Entities;

public sealed class ArticleTag
{
    public Guid NewsArticleId { get; set; }
    public Guid TagId { get; set; }

    // Navigation properties
    public NewsArticle NewsArticle { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
