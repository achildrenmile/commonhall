using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class EmailTemplate : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Content { get; set; } = "[]"; // JSONB blocks
    public string? ThumbnailUrl { get; set; }
    public bool IsSystem { get; set; }
    public EmailTemplateCategory Category { get; set; } = EmailTemplateCategory.Newsletter;

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public ICollection<EmailNewsletter> Newsletters { get; set; } = new List<EmailNewsletter>();
}
