using CommonHall.Domain.Entities;

namespace CommonHall.Application.Interfaces;

public interface IJourneyService
{
    Task DeliverNextStepAsync(JourneyEnrollment enrollment, CancellationToken cancellationToken = default);
    Task<JourneyAnalytics> GetAnalyticsAsync(Guid journeyId, CancellationToken cancellationToken = default);
    Task CompleteStepAsync(Guid enrollmentId, int stepIndex, CancellationToken cancellationToken = default);
    Task PauseEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
    Task ResumeEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
    Task CancelEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
}

public record JourneyAnalytics
{
    public int TotalEnrollments { get; init; }
    public int ActiveEnrollments { get; init; }
    public int CompletedEnrollments { get; init; }
    public int CancelledEnrollments { get; init; }
    public decimal CompletionRate { get; init; }
    public decimal AverageCompletionDays { get; init; }
    public IList<StepFunnelItem> StepFunnel { get; init; } = new List<StepFunnelItem>();
    public IList<EnrollmentTimelineItem> EnrollmentTimeline { get; init; } = new List<EnrollmentTimelineItem>();
}

public record StepFunnelItem
{
    public int StepIndex { get; init; }
    public string StepTitle { get; init; } = string.Empty;
    public int Delivered { get; init; }
    public int Completed { get; init; }
    public decimal CompletionRate { get; init; }
}

public record EnrollmentTimelineItem
{
    public DateTimeOffset Date { get; init; }
    public int NewEnrollments { get; init; }
    public int Completions { get; init; }
}
