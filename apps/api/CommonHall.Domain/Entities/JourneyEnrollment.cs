using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class JourneyEnrollment : BaseEntity
{
    public Guid JourneyId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int CurrentStepIndex { get; set; }
    public JourneyEnrollmentStatus Status { get; set; } = JourneyEnrollmentStatus.Active;
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? LastStepDeliveredAt { get; set; }

    // Navigation properties
    public Journey Journey { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<JourneyStepCompletion> StepCompletions { get; set; } = new List<JourneyStepCompletion>();
}
