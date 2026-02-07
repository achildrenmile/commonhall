using System.Text.Json;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.Services;

public sealed class JourneyTriggerService : IJourneyTriggerService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<JourneyTriggerService> _logger;

    public JourneyTriggerService(
        IApplicationDbContext context,
        ILogger<JourneyTriggerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Journey>> EvaluateTriggersForUserAsync(
        Guid userId,
        JourneyTriggerEvent triggerEvent,
        TriggerEventContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var matchingJourneys = new List<Journey>();

        // Get all active journeys with matching trigger types
        var triggerType = MapEventToTriggerType(triggerEvent);
        if (triggerType == null)
            return matchingJourneys;

        var candidateJourneys = await _context.Journeys
            .Where(j => j.IsActive && !j.IsDeleted && j.TriggerType == triggerType.Value)
            .ToListAsync(cancellationToken);

        foreach (var journey in candidateJourneys)
        {
            // Check if user is already enrolled
            var alreadyEnrolled = await _context.JourneyEnrollments
                .AnyAsync(e => e.JourneyId == journey.Id && e.UserId == userId, cancellationToken);

            if (alreadyEnrolled)
                continue;

            // Evaluate trigger config
            if (await EvaluateTriggerConfigAsync(journey, userId, context, cancellationToken))
            {
                matchingJourneys.Add(journey);
            }
        }

        return matchingJourneys;
    }

    public async Task EnrollUserInJourneyAsync(
        Guid userId,
        Guid journeyId,
        CancellationToken cancellationToken = default)
    {
        // Check if already enrolled
        var exists = await _context.JourneyEnrollments
            .AnyAsync(e => e.JourneyId == journeyId && e.UserId == userId, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("User {UserId} is already enrolled in journey {JourneyId}", userId, journeyId);
            return;
        }

        var journey = await _context.Journeys
            .Include(j => j.Steps)
            .FirstOrDefaultAsync(j => j.Id == journeyId && !j.IsDeleted, cancellationToken);

        if (journey == null || !journey.IsActive)
        {
            _logger.LogWarning("Journey {JourneyId} not found or inactive", journeyId);
            return;
        }

        var enrollment = new JourneyEnrollment
        {
            JourneyId = journeyId,
            UserId = userId,
            StartedAt = DateTimeOffset.UtcNow,
            CurrentStepIndex = 0,
            Status = JourneyEnrollmentStatus.Active
        };

        _context.JourneyEnrollments.Add(enrollment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} enrolled in journey {JourneyId}", userId, journeyId);
    }

    private static JourneyTriggerType? MapEventToTriggerType(JourneyTriggerEvent triggerEvent)
    {
        return triggerEvent switch
        {
            JourneyTriggerEvent.UserCreated => JourneyTriggerType.Onboarding,
            JourneyTriggerEvent.RoleChanged => JourneyTriggerType.RoleChange,
            JourneyTriggerEvent.LocationChanged => JourneyTriggerType.LocationChange,
            JourneyTriggerEvent.GroupJoined => JourneyTriggerType.GroupJoin,
            JourneyTriggerEvent.DateReached => JourneyTriggerType.DateBased,
            _ => null
        };
    }

    private async Task<bool> EvaluateTriggerConfigAsync(
        Journey journey,
        Guid userId,
        TriggerEventContext? context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(journey.TriggerConfig))
            return true; // No specific config means match all

        try
        {
            var config = JsonSerializer.Deserialize<TriggerConfig>(journey.TriggerConfig);
            if (config == null)
                return true;

            switch (journey.TriggerType)
            {
                case JourneyTriggerType.Onboarding:
                    // Onboarding triggers match all new users (optionally filtered by role/location)
                    return await EvaluateOnboardingTriggerAsync(config, userId, cancellationToken);

                case JourneyTriggerType.RoleChange:
                    if (context?.NewRole == null)
                        return false;
                    return config.TargetRoles?.Contains(context.NewRole, StringComparer.OrdinalIgnoreCase) ?? true;

                case JourneyTriggerType.LocationChange:
                    if (context?.NewLocation == null)
                        return false;
                    return config.TargetLocations?.Contains(context.NewLocation, StringComparer.OrdinalIgnoreCase) ?? true;

                case JourneyTriggerType.GroupJoin:
                    if (context?.GroupId == null)
                        return false;
                    return config.TargetGroupIds?.Contains(context.GroupId.Value) ?? true;

                case JourneyTriggerType.DateBased:
                    return await EvaluateDateBasedTriggerAsync(config, userId, cancellationToken);

                default:
                    return true;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse trigger config for journey {JourneyId}", journey.Id);
            return false;
        }
    }

    private async Task<bool> EvaluateOnboardingTriggerAsync(
        TriggerConfig config,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (config.TargetRoles?.Any() != true && config.TargetLocations?.Any() != true)
            return true;

        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.JobTitle, u.Location })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            return false;

        if (config.TargetRoles?.Any() == true)
        {
            if (!config.TargetRoles.Contains(user.JobTitle ?? "", StringComparer.OrdinalIgnoreCase))
                return false;
        }

        if (config.TargetLocations?.Any() == true)
        {
            if (!config.TargetLocations.Contains(user.Location ?? "", StringComparer.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private async Task<bool> EvaluateDateBasedTriggerAsync(
        TriggerConfig config,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(config.DateField))
            return false;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return false;

        DateTimeOffset? targetDate = config.DateField.ToLowerInvariant() switch
        {
            "hiredate" or "startdate" => user.HireDate,
            "birthday" => user.Birthday,
            _ => null
        };

        if (targetDate == null)
            return false;

        var today = DateTimeOffset.UtcNow.Date;
        var daysOffset = config.DaysOffset ?? 0;
        var triggerDate = targetDate.Value.AddDays(daysOffset).Date;

        return triggerDate == today;
    }
}

internal sealed class TriggerConfig
{
    public List<string>? TargetRoles { get; set; }
    public List<string>? TargetLocations { get; set; }
    public List<Guid>? TargetGroupIds { get; set; }
    public string? DateField { get; set; }
    public int? DaysOffset { get; set; }
}
