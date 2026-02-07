using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class CommunityPost : BaseEntity, ISoftDeletable
{
    public Guid CommunityId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsPinned { get; set; }
    public int LikeCount { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public Community Community { get; set; } = null!;
    public User Author { get; set; } = null!;
    public ICollection<CommunityPostComment> Comments { get; set; } = new List<CommunityPostComment>();
    public ICollection<CommunityPostReaction> Reactions { get; set; } = new List<CommunityPostReaction>();
}
