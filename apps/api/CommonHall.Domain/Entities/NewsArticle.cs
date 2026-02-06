using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class NewsArticle : BaseEntity, ISoftDeletable
{
    public Guid SpaceId { get; set; }
    public Guid? ChannelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? TeaserText { get; set; }
    public string? TeaserImageUrl { get; set; }
    public string Content { get; set; } = "[]";
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public Guid AuthorId { get; set; }
    public Guid? GhostAuthorId { get; set; }
    public bool IsPinned { get; set; }
    public bool AllowComments { get; set; } = true;
    public string? VisibilityRule { get; set; }
    public long ViewCount { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public Space Space { get; set; } = null!;
    public NewsChannel? Channel { get; set; }
    public User Author { get; set; } = null!;
    public User? GhostAuthor { get; set; }
    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
}
