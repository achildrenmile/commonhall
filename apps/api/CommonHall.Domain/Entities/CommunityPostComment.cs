using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class CommunityPostComment : BaseEntity, ISoftDeletable
{
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public CommunityPost Post { get; set; } = null!;
    public User Author { get; set; } = null!;
}
