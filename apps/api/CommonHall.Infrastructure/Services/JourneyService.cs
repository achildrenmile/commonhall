using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.Services;

public sealed class JourneyService : IJourneyService
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IEmailRenderer _emailRenderer;
    private readonly IEmailSendingService _emailService;
    private readonly ILogger<JourneyService> _logger;

    public JourneyService(
        IApplicationDbContext context,
        INotificationService notificationService,
        IEmailRenderer emailRenderer,
        IEmailSendingService emailService,
        ILogger<JourneyService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _emailRenderer = emailRenderer;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task DeliverNextStepAsync(JourneyEnrollment enrollment, CancellationToken cancellationToken = default)
    {
        var journey = await _context.Journeys
            .Include(j => j.Steps)
            .FirstOrDefaultAsync(j => j.Id == enrollment.JourneyId, cancellationToken);

        if (journey == null)
            return;

        var steps = journey.Steps.OrderBy(s => s.SortOrder).ToList();
        if (enrollment.CurrentStepIndex >= steps.Count)
        {
            // All steps completed
            enrollment.Status = JourneyEnrollmentStatus.Completed;
            enrollment.CompletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var currentStep = steps[enrollment.CurrentStepIndex];

        // Get user info for delivery
        var user = await _context.Users
            .Where(u => u.Id == enrollment.UserId)
            .Select(u => new { u.Email, u.FirstName })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            return;

        // Deliver based on channel type
        var deliveredVia = JourneyChannelType.AppNotification;

        if (currentStep.ChannelType is JourneyChannelType.AppNotification or JourneyChannelType.Both)
        {
            await _notificationService.SendAsync(
                enrollment.UserId,
                "Journey Step",
                currentStep.Title,
                currentStep.Description,
                $"/journeys/{enrollment.Id}/steps/{enrollment.CurrentStepIndex}",
                cancellationToken);
            deliveredVia = JourneyChannelType.AppNotification;
        }

        if (currentStep.ChannelType is JourneyChannelType.Email or JourneyChannelType.Both)
        {
            var emailHtml = RenderStepEmail(currentStep, user.FirstName ?? "there");
            await _emailService.SendAsync(
                user.Email ?? "",
                $"Journey: {currentStep.Title}",
                emailHtml,
                null,
                cancellationToken);
            deliveredVia = currentStep.ChannelType == JourneyChannelType.Both
                ? JourneyChannelType.Both
                : JourneyChannelType.Email;
        }

        // Create completion record
        var completion = new JourneyStepCompletion
        {
            EnrollmentId = enrollment.Id,
            StepIndex = enrollment.CurrentStepIndex,
            DeliveredAt = DateTimeOffset.UtcNow,
            DeliveryChannel = deliveredVia
        };

        _context.JourneyStepCompletions.Add(completion);

        enrollment.LastStepDeliveredAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Delivered step {StepIndex} of journey {JourneyId} to user {UserId}",
            enrollment.CurrentStepIndex, enrollment.JourneyId, enrollment.UserId);
    }

    public async Task CompleteStepAsync(Guid enrollmentId, int stepIndex, CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.JourneyEnrollments
            .Include(e => e.StepCompletions)
            .Include(e => e.Journey)
            .ThenInclude(j => j.Steps)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken);

        if (enrollment == null || enrollment.Status != JourneyEnrollmentStatus.Active)
            return;

        var completion = enrollment.StepCompletions.FirstOrDefault(c => c.StepIndex == stepIndex);
        if (completion == null || completion.CompletedAt.HasValue)
            return;

        completion.CompletedAt = DateTimeOffset.UtcNow;

        // Check if this was the current step and advance
        if (enrollment.CurrentStepIndex == stepIndex)
        {
            var totalSteps = enrollment.Journey.Steps.Count;
            enrollment.CurrentStepIndex++;

            if (enrollment.CurrentStepIndex >= totalSteps)
            {
                enrollment.Status = JourneyEnrollmentStatus.Completed;
                enrollment.CompletedAt = DateTimeOffset.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User completed step {StepIndex} of enrollment {EnrollmentId}",
            stepIndex, enrollmentId);
    }

    public async Task PauseEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.JourneyEnrollments
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken);

        if (enrollment == null || enrollment.Status != JourneyEnrollmentStatus.Active)
            return;

        enrollment.Status = JourneyEnrollmentStatus.Paused;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ResumeEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.JourneyEnrollments
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken);

        if (enrollment == null || enrollment.Status != JourneyEnrollmentStatus.Paused)
            return;

        enrollment.Status = JourneyEnrollmentStatus.Active;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.JourneyEnrollments
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken);

        if (enrollment == null || enrollment.Status is JourneyEnrollmentStatus.Completed or JourneyEnrollmentStatus.Cancelled)
            return;

        enrollment.Status = JourneyEnrollmentStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<JourneyAnalytics> GetAnalyticsAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        var journey = await _context.Journeys
            .Include(j => j.Steps)
            .Include(j => j.Enrollments)
            .ThenInclude(e => e.StepCompletions)
            .FirstOrDefaultAsync(j => j.Id == journeyId, cancellationToken);

        if (journey == null)
            return new JourneyAnalytics();

        var enrollments = journey.Enrollments.ToList();
        var steps = journey.Steps.OrderBy(s => s.SortOrder).ToList();

        var totalEnrollments = enrollments.Count;
        var activeEnrollments = enrollments.Count(e => e.Status == JourneyEnrollmentStatus.Active);
        var completedEnrollments = enrollments.Count(e => e.Status == JourneyEnrollmentStatus.Completed);
        var cancelledEnrollments = enrollments.Count(e => e.Status == JourneyEnrollmentStatus.Cancelled);

        var completionRate = totalEnrollments > 0
            ? Math.Round((decimal)completedEnrollments / totalEnrollments * 100, 1)
            : 0;

        // Calculate average completion time
        var completedWithDuration = enrollments
            .Where(e => e.Status == JourneyEnrollmentStatus.Completed && e.CompletedAt.HasValue)
            .Select(e => (e.CompletedAt!.Value - e.StartedAt).TotalDays)
            .ToList();

        var avgCompletionDays = completedWithDuration.Any()
            ? Math.Round((decimal)completedWithDuration.Average(), 1)
            : 0;

        // Step funnel
        var stepFunnel = steps.Select((step, index) => new StepFunnelItem
        {
            StepIndex = index,
            StepTitle = step.Title,
            Delivered = enrollments
                .SelectMany(e => e.StepCompletions)
                .Count(c => c.StepIndex == index),
            Completed = enrollments
                .SelectMany(e => e.StepCompletions)
                .Count(c => c.StepIndex == index && c.CompletedAt.HasValue)
        }).Select(f => f with
        {
            CompletionRate = f.Delivered > 0 ? Math.Round((decimal)f.Completed / f.Delivered * 100, 1) : 0
        }).ToList();

        // Enrollment timeline (last 30 days)
        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
        var timeline = Enumerable.Range(0, 30)
            .Select(i => DateTimeOffset.UtcNow.Date.AddDays(-i))
            .Select(date => new EnrollmentTimelineItem
            {
                Date = date,
                NewEnrollments = enrollments.Count(e => e.StartedAt.Date == date),
                Completions = enrollments.Count(e => e.CompletedAt?.Date == date)
            })
            .OrderBy(t => t.Date)
            .ToList();

        return new JourneyAnalytics
        {
            TotalEnrollments = totalEnrollments,
            ActiveEnrollments = activeEnrollments,
            CompletedEnrollments = completedEnrollments,
            CancelledEnrollments = cancelledEnrollments,
            CompletionRate = completionRate,
            AverageCompletionDays = avgCompletionDays,
            StepFunnel = stepFunnel,
            EnrollmentTimeline = timeline
        };
    }

    private static string RenderStepEmail(JourneyStep step, string userName)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
            </head>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; margin: 0; padding: 0; background-color: #f8fafc;">
                <table width="100%" cellpadding="0" cellspacing="0" style="max-width: 600px; margin: 0 auto; padding: 20px;">
                    <tr>
                        <td style="background-color: #ffffff; border-radius: 8px; padding: 32px;">
                            <h1 style="color: #1e293b; font-size: 24px; margin: 0 0 16px 0;">Hi {userName}!</h1>
                            <h2 style="color: #334155; font-size: 20px; margin: 0 0 12px 0;">{step.Title}</h2>
                            {(string.IsNullOrEmpty(step.Description) ? "" : $"<p style=\"color: #64748b; font-size: 16px; line-height: 1.6; margin: 0 0 24px 0;\">{step.Description}</p>")}
                            <p style="color: #64748b; font-size: 14px; margin: 24px 0 0 0;">
                                Complete this step to continue your journey.
                            </p>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }
}
