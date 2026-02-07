using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class JourneyStepCompletion : BaseEntity
{
    public Guid EnrollmentId { get; set; }
    public int StepIndex { get; set; }
    public DateTimeOffset DeliveredAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public JourneyChannelType DeliveryChannel { get; set; }
    public DateTimeOffset? ViewedAt { get; set; }

    // Navigation properties
    public JourneyEnrollment Enrollment { get; set; } = null!;
}
