using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Api.Controllers;

[Authorize]
public class JourneysController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IJourneyTriggerService _triggerService;
    private readonly IJourneyService _journeyService;

    public JourneysController(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IJourneyTriggerService triggerService,
        IJourneyService journeyService)
    {
        _context = context;
        _currentUser = currentUser;
        _triggerService = triggerService;
        _journeyService = journeyService;
    }

    /// <summary>
    /// Get all journeys with optional filtering.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? activeOnly,
        [FromQuery] JourneyTriggerType? triggerType,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var query = _context.Journeys
            .Include(j => j.Steps)
            .Include(j => j.Enrollments)
            .Where(j => !j.IsDeleted)
            .AsQueryable();

        if (activeOnly == true)
            query = query.Where(j => j.IsActive);

        if (triggerType.HasValue)
            query = query.Where(j => j.TriggerType == triggerType.Value);

        var journeys = await query
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new JourneyListDto
            {
                Id = j.Id,
                Name = j.Name,
                Description = j.Description,
                TriggerType = j.TriggerType,
                IsActive = j.IsActive,
                StepCount = j.Steps.Count,
                EnrollmentCount = j.Enrollments.Count,
                CompletionRate = j.Enrollments.Count > 0
                    ? Math.Round((decimal)j.Enrollments.Count(e => e.Status == JourneyEnrollmentStatus.Completed) / j.Enrollments.Count * 100, 1)
                    : 0,
                CreatedAt = j.CreatedAt,
                UpdatedAt = j.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(journeys);
    }

    /// <summary>
    /// Get a single journey by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var journey = await _context.Journeys
            .Include(j => j.Steps)
            .Include(j => j.Space)
            .Where(j => j.Id == id && !j.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (journey == null)
            return NotFound();

        var enrollmentStats = await _context.JourneyEnrollments
            .Where(e => e.JourneyId == id)
            .GroupBy(e => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(e => e.Status == JourneyEnrollmentStatus.Active),
                Completed = g.Count(e => e.Status == JourneyEnrollmentStatus.Completed)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new JourneyDetailDto
        {
            Id = journey.Id,
            Name = journey.Name,
            Description = journey.Description,
            TriggerType = journey.TriggerType,
            TriggerConfig = journey.TriggerConfig,
            IsActive = journey.IsActive,
            SpaceId = journey.SpaceId,
            SpaceName = journey.Space?.Name,
            Steps = journey.Steps
                .OrderBy(s => s.SortOrder)
                .Select(s => new JourneyStepDto
                {
                    Id = s.Id,
                    SortOrder = s.SortOrder,
                    Title = s.Title,
                    Description = s.Description,
                    Content = s.Content,
                    DelayDays = s.DelayDays,
                    ChannelType = s.ChannelType,
                    IsRequired = s.IsRequired
                })
                .ToList(),
            EnrollmentCount = enrollmentStats?.Total ?? 0,
            ActiveEnrollments = enrollmentStats?.Active ?? 0,
            CompletedEnrollments = enrollmentStats?.Completed ?? 0,
            CreatedAt = journey.CreatedAt,
            UpdatedAt = journey.UpdatedAt
        });
    }

    /// <summary>
    /// Create a new journey.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateJourneyRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var journey = new Journey
        {
            Name = request.Name,
            Description = request.Description,
            TriggerType = request.TriggerType ?? JourneyTriggerType.Manual,
            TriggerConfig = request.TriggerConfig,
            IsActive = false,
            SpaceId = request.SpaceId
        };

        _context.Journeys.Add(journey);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = journey.Id }, new JourneyDetailDto
        {
            Id = journey.Id,
            Name = journey.Name,
            Description = journey.Description,
            TriggerType = journey.TriggerType,
            TriggerConfig = journey.TriggerConfig,
            IsActive = journey.IsActive,
            SpaceId = journey.SpaceId,
            Steps = new List<JourneyStepDto>(),
            CreatedAt = journey.CreatedAt,
            UpdatedAt = journey.UpdatedAt
        });
    }

    /// <summary>
    /// Update a journey.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateJourneyRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var journey = await _context.Journeys
            .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted, cancellationToken);

        if (journey == null)
            return NotFound();

        if (request.Name != null)
            journey.Name = request.Name;
        if (request.Description != null)
            journey.Description = request.Description;
        if (request.TriggerType.HasValue)
            journey.TriggerType = request.TriggerType.Value;
        if (request.TriggerConfig != null)
            journey.TriggerConfig = request.TriggerConfig;
        if (request.SpaceId.HasValue)
            journey.SpaceId = request.SpaceId == Guid.Empty ? null : request.SpaceId;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Journey updated" });
    }

    /// <summary>
    /// Update journey steps (bulk).
    /// </summary>
    [HttpPut("{id:guid}/steps")]
    public async Task<IActionResult> UpdateSteps(
        Guid id,
        [FromBody] UpdateJourneyStepsRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var journey = await _context.Journeys
            .Include(j => j.Steps)
            .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted, cancellationToken);

        if (journey == null)
            return NotFound();

        // Remove steps that are no longer in the list
        var newStepIds = request.Steps.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();
        var stepsToRemove = journey.Steps.Where(s => !newStepIds.Contains(s.Id)).ToList();
        foreach (var step in stepsToRemove)
        {
            _context.JourneySteps.Remove(step);
        }

        // Update existing and add new
        for (int i = 0; i < request.Steps.Count; i++)
        {
            var stepInput = request.Steps[i];

            if (stepInput.Id.HasValue)
            {
                var existingStep = journey.Steps.FirstOrDefault(s => s.Id == stepInput.Id.Value);
                if (existingStep != null)
                {
                    existingStep.SortOrder = i;
                    existingStep.Title = stepInput.Title;
                    existingStep.Description = stepInput.Description;
                    existingStep.Content = stepInput.Content ?? "[]";
                    existingStep.DelayDays = stepInput.DelayDays ?? 0;
                    existingStep.ChannelType = stepInput.ChannelType ?? JourneyChannelType.Both;
                    existingStep.IsRequired = stepInput.IsRequired ?? true;
                }
            }
            else
            {
                var newStep = new JourneyStep
                {
                    JourneyId = id,
                    SortOrder = i,
                    Title = stepInput.Title,
                    Description = stepInput.Description,
                    Content = stepInput.Content ?? "[]",
                    DelayDays = stepInput.DelayDays ?? 0,
                    ChannelType = stepInput.ChannelType ?? JourneyChannelType.Both,
                    IsRequired = stepInput.IsRequired ?? true
                };
                _context.JourneySteps.Add(newStep);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Steps updated" });
    }

    /// <summary>
    /// Activate a journey.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var journey = await _context.Journeys
            .Include(j => j.Steps)
            .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted, cancellationToken);

        if (journey == null)
            return NotFound();

        if (journey.Steps.Count == 0)
            return BadRequest(new { error = "Journey must have at least one step before activation" });

        journey.IsActive = true;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Journey activated" });
    }

    /// <summary>
    /// Deactivate a journey.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var journey = await _context.Journeys
            .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted, cancellationToken);

        if (journey == null)
            return NotFound();

        journey.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Journey deactivated" });
    }

    /// <summary>
    /// Manually enroll a user in a journey.
    /// </summary>
    [HttpPost("{id:guid}/enroll")]
    public async Task<IActionResult> Enroll(
        Guid id,
        [FromBody] EnrollUserRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var journey = await _context.Journeys
            .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted && j.IsActive, cancellationToken);

        if (journey == null)
            return NotFound();

        await _triggerService.EnrollUserInJourneyAsync(request.UserId, id, cancellationToken);

        return Ok(new { message = "User enrolled" });
    }

    /// <summary>
    /// Get journey enrollments.
    /// </summary>
    [HttpGet("{id:guid}/enrollments")]
    public async Task<IActionResult> GetEnrollments(
        Guid id,
        [FromQuery] JourneyEnrollmentStatus? status,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var query = _context.JourneyEnrollments
            .Include(e => e.User)
            .Include(e => e.StepCompletions)
            .Where(e => e.JourneyId == id)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId))
        {
            var cursorEnrollment = await _context.JourneyEnrollments
                .FirstOrDefaultAsync(e => e.Id == cursorId, cancellationToken);

            if (cursorEnrollment != null)
            {
                query = query.Where(e => e.StartedAt < cursorEnrollment.StartedAt);
            }
        }

        var enrollments = await query
            .OrderByDescending(e => e.StartedAt)
            .Take(limit + 1)
            .Select(e => new EnrollmentListDto
            {
                Id = e.Id,
                UserId = e.UserId,
                UserName = $"{e.User.FirstName} {e.User.LastName}".Trim(),
                UserEmail = e.User.Email ?? "",
                Status = e.Status,
                CurrentStepIndex = e.CurrentStepIndex,
                StepsCompleted = e.StepCompletions.Count(c => c.CompletedAt.HasValue),
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt
            })
            .ToListAsync(cancellationToken);

        var hasMore = enrollments.Count > limit;
        if (hasMore)
            enrollments = enrollments.Take(limit).ToList();

        return Ok(new
        {
            items = enrollments,
            nextCursor = hasMore ? enrollments.Last().Id.ToString() : null,
            hasMore
        });
    }

    /// <summary>
    /// Get journey analytics.
    /// </summary>
    [HttpGet("{id:guid}/analytics")]
    public async Task<IActionResult> GetAnalytics(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var journey = await _context.Journeys
            .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted, cancellationToken);

        if (journey == null)
            return NotFound();

        var analytics = await _journeyService.GetAnalyticsAsync(id, cancellationToken);
        return Ok(analytics);
    }

    /// <summary>
    /// Delete a journey.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var journey = await _context.Journeys
            .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted, cancellationToken);

        if (journey == null)
            return NotFound();

        journey.IsDeleted = true;
        journey.DeletedAt = DateTimeOffset.UtcNow;
        journey.DeletedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

// DTOs
public record JourneyListDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public JourneyTriggerType TriggerType { get; init; }
    public bool IsActive { get; init; }
    public int StepCount { get; init; }
    public int EnrollmentCount { get; init; }
    public decimal CompletionRate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public record JourneyDetailDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public JourneyTriggerType TriggerType { get; init; }
    public string? TriggerConfig { get; init; }
    public bool IsActive { get; init; }
    public Guid? SpaceId { get; init; }
    public string? SpaceName { get; init; }
    public IList<JourneyStepDto> Steps { get; init; } = new List<JourneyStepDto>();
    public int EnrollmentCount { get; init; }
    public int ActiveEnrollments { get; init; }
    public int CompletedEnrollments { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public record JourneyStepDto
{
    public Guid Id { get; init; }
    public int SortOrder { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Content { get; init; }
    public int DelayDays { get; init; }
    public JourneyChannelType ChannelType { get; init; }
    public bool IsRequired { get; init; }
}

public record EnrollmentListDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public required string UserName { get; init; }
    public required string UserEmail { get; init; }
    public JourneyEnrollmentStatus Status { get; init; }
    public int CurrentStepIndex { get; init; }
    public int StepsCompleted { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}

public record CreateJourneyRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public JourneyTriggerType? TriggerType { get; init; }
    public string? TriggerConfig { get; init; }
    public Guid? SpaceId { get; init; }
}

public record UpdateJourneyRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public JourneyTriggerType? TriggerType { get; init; }
    public string? TriggerConfig { get; init; }
    public Guid? SpaceId { get; init; }
}

public record UpdateJourneyStepsRequest
{
    public IList<JourneyStepInput> Steps { get; init; } = new List<JourneyStepInput>();
}

public record JourneyStepInput
{
    public Guid? Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Content { get; init; }
    public int? DelayDays { get; init; }
    public JourneyChannelType? ChannelType { get; init; }
    public bool? IsRequired { get; init; }
}

public record EnrollUserRequest
{
    public Guid UserId { get; init; }
}
