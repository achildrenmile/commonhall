using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class Form : BaseEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Fields { get; set; } = "[]"; // JSONB array of field definitions
    public string? NotificationEmail { get; set; }
    public string? ConfirmationMessage { get; set; }
    public bool IsActive { get; set; }
    public Guid? SpaceId { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public Space? Space { get; set; }
    public ICollection<FormSubmission> Submissions { get; set; } = new List<FormSubmission>();
}
