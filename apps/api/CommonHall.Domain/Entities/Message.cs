using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? Attachments { get; set; } // JSONB array

    // Navigation properties
    public Conversation Conversation { get; set; } = null!;
    public User Author { get; set; } = null!;
}
