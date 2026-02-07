using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class EmailNewsletter : BaseEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? PreviewText { get; set; }
    public string Content { get; set; } = "[]"; // JSONB blocks
    public Guid? TemplateId { get; set; }
    public NewsletterStatus Status { get; set; } = NewsletterStatus.Draft;
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DistributionType DistributionType { get; set; } = DistributionType.AllUsers;
    public string? TargetGroupIds { get; set; } // JSONB array of group IDs

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public EmailTemplate? Template { get; set; }
    public ICollection<EmailRecipient> Recipients { get; set; } = new List<EmailRecipient>();
}
