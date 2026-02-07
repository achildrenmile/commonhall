using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class Journey : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public JourneyTriggerType TriggerType { get; set; } = JourneyTriggerType.Manual;
    public string? TriggerConfig { get; set; } // JSONB configuration for trigger
    public bool IsActive { get; set; }
    public Guid? SpaceId { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public Space? Space { get; set; }
    public ICollection<JourneyStep> Steps { get; set; } = new List<JourneyStep>();
    public ICollection<JourneyEnrollment> Enrollments { get; set; } = new List<JourneyEnrollment>();
}
