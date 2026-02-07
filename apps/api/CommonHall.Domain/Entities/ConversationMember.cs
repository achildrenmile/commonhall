using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class ConversationMember : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LastReadAt { get; set; }
    public bool IsMuted { get; set; }

    // Navigation properties
    public Conversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
}
