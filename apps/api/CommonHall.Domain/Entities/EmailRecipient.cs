using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class EmailRecipient : BaseEntity
{
    public Guid NewsletterId { get; set; }
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TrackingToken { get; set; } = string.Empty; // Unique token for tracking
    public EmailRecipientStatus Status { get; set; } = EmailRecipientStatus.Pending;
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? OpenedAt { get; set; }
    public DateTimeOffset? ClickedAt { get; set; }
    public int OpenCount { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public EmailNewsletter Newsletter { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<EmailClick> Clicks { get; set; } = new List<EmailClick>();
}
