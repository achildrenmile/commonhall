using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class Comment : BaseEntity, ISoftDeletable
{
    public Guid NewsArticleId { get; set; }
    public Guid AuthorId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsModerated { get; set; }
    public decimal? SentimentScore { get; set; }
    public string? SentimentLabel { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public NewsArticle NewsArticle { get; set; } = null!;
    public User Author { get; set; } = null!;
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
