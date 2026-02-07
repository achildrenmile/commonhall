using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class Community : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public CommunityType Type { get; set; }
    public string? AssignedGroupIds { get; set; } // JSONB array of group IDs
    public CommunityPostPermission PostPermission { get; set; }
    public int MemberCount { get; set; }
    public bool IsArchived { get; set; }
    public Guid? SpaceId { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public Space? Space { get; set; }
    public ICollection<CommunityMembership> Memberships { get; set; } = new List<CommunityMembership>();
    public ICollection<CommunityPost> Posts { get; set; } = new List<CommunityPost>();
}
