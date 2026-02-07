using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class CommunityMembership : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid UserId { get; set; }
    public CommunityMemberRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; }

    // Navigation properties
    public Community Community { get; set; } = null!;
    public User User { get; set; } = null!;
}
