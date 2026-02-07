namespace CommonHall.Domain.Enums;

public enum CommunityType
{
    Open = 0,
    Closed = 1,
    Assigned = 2
}

public enum CommunityPostPermission
{
    Anyone = 0,
    MembersOnly = 1,
    AdminsOnly = 2
}

public enum CommunityMemberRole
{
    Member = 0,
    Moderator = 1,
    Admin = 2
}

public enum ConversationType
{
    Direct = 0,
    Group = 1,
    Managed = 2
}
