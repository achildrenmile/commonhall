using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;

namespace CommonHall.Application.Interfaces;

public interface IJourneyTriggerService
{
    Task<IEnumerable<Journey>> EvaluateTriggersForUserAsync(
        Guid userId,
        JourneyTriggerEvent triggerEvent,
        TriggerEventContext? context = null,
        CancellationToken cancellationToken = default);

    Task EnrollUserInJourneyAsync(
        Guid userId,
        Guid journeyId,
        CancellationToken cancellationToken = default);
}

public record TriggerEventContext
{
    public string? PreviousRole { get; init; }
    public string? NewRole { get; init; }
    public string? PreviousLocation { get; init; }
    public string? NewLocation { get; init; }
    public Guid? GroupId { get; init; }
    public string? GroupName { get; init; }
}
