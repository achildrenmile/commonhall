namespace CommonHall.Domain.Enums;

public enum JourneyTriggerType
{
    Manual = 0,
    Onboarding = 1,
    RoleChange = 2,
    LocationChange = 3,
    DateBased = 4,
    GroupJoin = 5
}

public enum JourneyChannelType
{
    AppNotification = 0,
    Email = 1,
    Both = 2
}

public enum JourneyEnrollmentStatus
{
    Active = 0,
    Paused = 1,
    Completed = 2,
    Cancelled = 3
}

public enum JourneyTriggerEvent
{
    UserCreated = 0,
    UserUpdated = 1,
    RoleChanged = 2,
    LocationChanged = 3,
    GroupJoined = 4,
    GroupLeft = 5,
    DateReached = 6
}
