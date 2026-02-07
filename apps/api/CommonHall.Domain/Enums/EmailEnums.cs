namespace CommonHall.Domain.Enums;

public enum NewsletterStatus
{
    Draft = 0,
    Scheduled = 1,
    Sending = 2,
    Sent = 3,
    Failed = 4
}

public enum DistributionType
{
    AllUsers = 0,
    UserGroups = 1,
    CustomList = 2
}

public enum EmailRecipientStatus
{
    Pending = 0,
    Sent = 1,
    Delivered = 2,
    Bounced = 3,
    Failed = 4
}

public enum EmailTemplateCategory
{
    Newsletter = 0,
    Announcement = 1,
    Digest = 2,
    Alert = 3,
    Welcome = 4,
    Custom = 5
}
