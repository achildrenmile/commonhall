namespace CommonHall.Domain.Entities;

public sealed class UserGroupMembership
{
    public Guid UserId { get; set; }
    public Guid UserGroupId { get; set; }
    public DateTimeOffset JoinedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public UserGroup UserGroup { get; set; } = null!;
}
