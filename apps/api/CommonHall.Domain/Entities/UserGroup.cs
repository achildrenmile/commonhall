using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class UserGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GroupType Type { get; set; } = GroupType.Manual;
    public string? RuleDefinition { get; set; }
    public bool IsSystem { get; set; }

    // Navigation properties
    public ICollection<UserGroupMembership> Memberships { get; set; } = new List<UserGroupMembership>();
}
