using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class CommunityPostReaction : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = "like"; // like, love, celebrate, etc.

    // Navigation properties
    public CommunityPost Post { get; set; } = null!;
    public User User { get; set; } = null!;
}
