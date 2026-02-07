using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class Conversation : BaseEntity
{
    public ConversationType Type { get; set; }
    public string? Name { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }
    public string? LastMessagePreview { get; set; }

    // Navigation properties
    public User CreatedBy { get; set; } = null!;
    public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
