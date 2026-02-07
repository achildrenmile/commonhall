using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Api.Controllers;

[Authorize]
[Route("api/v1/me/journeys")]
public class MyJourneysController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IJourneyService _journeyService;

    public MyJourneysController(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IJourneyService journeyService)
    {
        _context = context;
        _currentUser = currentUser;
        _journeyService = journeyService;
    }

    /// <summary>
    /// Get current user's journey enrollments.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyJourneys(
        [FromQuery] JourneyEnrollmentStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _context.JourneyEnrollments
            .Include(e => e.Journey)
            .ThenInclude(j => j.Steps)
            .Include(e => e.StepCompletions)
            .Where(e => e.UserId == _currentUser.UserId)
            .Where(e => !e.Journey.IsDeleted)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        var enrollments = await query
            .OrderByDescending(e => e.StartedAt)
            .Select(e => new MyJourneyDto
            {
                EnrollmentId = e.Id,
                JourneyId = e.JourneyId,
                JourneyName = e.Journey.Name,
                JourneyDescription = e.Journey.Description,
                Status = e.Status,
                TotalSteps = e.Journey.Steps.Count,
                CompletedSteps = e.StepCompletions.Count(c => c.CompletedAt.HasValue),
                CurrentStepIndex = e.CurrentStepIndex,
                ProgressPercent = e.Journey.Steps.Count > 0
                    ? Math.Round((decimal)e.StepCompletions.Count(c => c.CompletedAt.HasValue) / e.Journey.Steps.Count * 100, 0)
                    : 0,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(enrollments);
    }

    /// <summary>
    /// Get detailed view of a specific enrollment.
    /// </summary>
    [HttpGet("{enrollmentId:guid}")]
    public async Task<IActionResult> GetJourneyDetail(Guid enrollmentId, CancellationToken cancellationToken)
    {
        var enrollment = await _context.JourneyEnrollments
            .Include(e => e.Journey)
            .ThenInclude(j => j.Steps)
            .Include(e => e.StepCompletions)
            .Where(e => e.Id == enrollmentId && e.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (enrollment == null)
            return NotFound();

        var steps = enrollment.Journey.Steps
            .OrderBy(s => s.SortOrder)
            .Select(s =>
            {
                var completion = enrollment.StepCompletions.FirstOrDefault(c => c.StepIndex == s.SortOrder);
                return new MyJourneyStepDto
                {
                    StepIndex = s.SortOrder,
                    Title = s.Title,
                    Description = s.Description,
                    Content = s.Content,
                    DelayDays = s.DelayDays,
                    IsRequired = s.IsRequired,
                    IsDelivered = completion != null,
                    IsCompleted = completion?.CompletedAt.HasValue ?? false,
                    IsCurrentStep = s.SortOrder == enrollment.CurrentStepIndex,
                    DeliveredAt = completion?.DeliveredAt,
                    CompletedAt = completion?.CompletedAt,
                    ViewedAt = completion?.ViewedAt
                };
            })
            .ToList();

        return Ok(new MyJourneyDetailDto
        {
            EnrollmentId = enrollment.Id,
            JourneyId = enrollment.JourneyId,
            JourneyName = enrollment.Journey.Name,
            JourneyDescription = enrollment.Journey.Description,
            Status = enrollment.Status,
            CurrentStepIndex = enrollment.CurrentStepIndex,
            TotalSteps = steps.Count,
            CompletedSteps = steps.Count(s => s.IsCompleted),
            ProgressPercent = steps.Count > 0
                ? Math.Round((decimal)steps.Count(s => s.IsCompleted) / steps.Count * 100, 0)
                : 0,
            StartedAt = enrollment.StartedAt,
            CompletedAt = enrollment.CompletedAt,
            Steps = steps
        });
    }

    /// <summary>
    /// Mark a step as viewed.
    /// </summary>
    [HttpPost("{enrollmentId:guid}/steps/{stepIndex:int}/view")]
    public async Task<IActionResult> MarkStepViewed(
        Guid enrollmentId,
        int stepIndex,
        CancellationToken cancellationToken)
    {
        var enrollment = await _context.JourneyEnrollments
            .Include(e => e.StepCompletions)
            .Where(e => e.Id == enrollmentId && e.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (enrollment == null)
            return NotFound();

        var completion = enrollment.StepCompletions.FirstOrDefault(c => c.StepIndex == stepIndex);
        if (completion == null)
            return BadRequest(new { error = "Step has not been delivered yet" });

        if (!completion.ViewedAt.HasValue)
        {
            completion.ViewedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { message = "Step marked as viewed" });
    }

    /// <summary>
    /// Complete a step.
    /// </summary>
    [HttpPost("{enrollmentId:guid}/steps/{stepIndex:int}/complete")]
    public async Task<IActionResult> CompleteStep(
        Guid enrollmentId,
        int stepIndex,
        CancellationToken cancellationToken)
    {
        var enrollment = await _context.JourneyEnrollments
            .Where(e => e.Id == enrollmentId && e.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (enrollment == null)
            return NotFound();

        if (enrollment.Status != JourneyEnrollmentStatus.Active)
            return BadRequest(new { error = "Enrollment is not active" });

        await _journeyService.CompleteStepAsync(enrollmentId, stepIndex, cancellationToken);

        return Ok(new { message = "Step completed" });
    }

    /// <summary>
    /// Pause enrollment.
    /// </summary>
    [HttpPost("{enrollmentId:guid}/pause")]
    public async Task<IActionResult> Pause(Guid enrollmentId, CancellationToken cancellationToken)
    {
        var enrollment = await _context.JourneyEnrollments
            .Where(e => e.Id == enrollmentId && e.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (enrollment == null)
            return NotFound();

        await _journeyService.PauseEnrollmentAsync(enrollmentId, cancellationToken);

        return Ok(new { message = "Journey paused" });
    }

    /// <summary>
    /// Resume enrollment.
    /// </summary>
    [HttpPost("{enrollmentId:guid}/resume")]
    public async Task<IActionResult> Resume(Guid enrollmentId, CancellationToken cancellationToken)
    {
        var enrollment = await _context.JourneyEnrollments
            .Where(e => e.Id == enrollmentId && e.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (enrollment == null)
            return NotFound();

        await _journeyService.ResumeEnrollmentAsync(enrollmentId, cancellationToken);

        return Ok(new { message = "Journey resumed" });
    }
}

// DTOs
public record MyJourneyDto
{
    public Guid EnrollmentId { get; init; }
    public Guid JourneyId { get; init; }
    public required string JourneyName { get; init; }
    public string? JourneyDescription { get; init; }
    public JourneyEnrollmentStatus Status { get; init; }
    public int TotalSteps { get; init; }
    public int CompletedSteps { get; init; }
    public int CurrentStepIndex { get; init; }
    public decimal ProgressPercent { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}

public record MyJourneyDetailDto : MyJourneyDto
{
    public IList<MyJourneyStepDto> Steps { get; init; } = new List<MyJourneyStepDto>();
}

public record MyJourneyStepDto
{
    public int StepIndex { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Content { get; init; }
    public int DelayDays { get; init; }
    public bool IsRequired { get; init; }
    public bool IsDelivered { get; init; }
    public bool IsCompleted { get; init; }
    public bool IsCurrentStep { get; init; }
    public DateTimeOffset? DeliveredAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? ViewedAt { get; init; }
}
