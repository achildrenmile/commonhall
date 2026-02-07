using CommonHall.Domain.ValueObjects;

namespace CommonHall.Application.Interfaces;

/// <summary>
/// Service for evaluating visibility rules and filtering content based on user targeting.
/// </summary>
public interface ITargetingService
{
    /// <summary>
    /// Checks if content with the given visibility rule is visible to the specified user.
    /// </summary>
    /// <param name="userId">The user ID to check visibility for.</param>
    /// <param name="visibilityRuleJson">The visibility rule JSON. Null or empty means visible to all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the content is visible to the user.</returns>
    Task<bool> IsVisibleAsync(Guid userId, string? visibilityRuleJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Filters a collection of items based on their visibility rules.
    /// </summary>
    /// <typeparam name="T">The type of items to filter.</typeparam>
    /// <param name="userId">The user ID to filter for.</param>
    /// <param name="items">The items to filter.</param>
    /// <param name="ruleSelector">A function to extract the visibility rule JSON from each item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Items that are visible to the user.</returns>
    Task<IEnumerable<T>> FilterVisibleAsync<T>(
        Guid userId,
        IEnumerable<T> items,
        Func<T, string?> ruleSelector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a visibility rule against a user.
    /// </summary>
    /// <param name="userId">The user ID to evaluate against.</param>
    /// <param name="rule">The parsed visibility rule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the rule evaluates to visible for the user.</returns>
    Task<bool> EvaluateRuleAsync(Guid userId, VisibilityRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a preview of users matching a visibility rule.
    /// </summary>
    /// <param name="visibilityRuleJson">The visibility rule JSON.</param>
    /// <param name="limit">Maximum number of sample users to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count and sample of matching users.</returns>
    Task<TargetingPreview> GetPreviewAsync(string? visibilityRuleJson, int limit = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Filters widgets in a content JSON array based on their visibility rules.
    /// </summary>
    /// <param name="contentJson">The content JSON containing widgets.</param>
    /// <param name="userId">The user ID to filter for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Filtered content JSON with only visible widgets.</returns>
    Task<string> FilterWidgetsAsync(string contentJson, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cached user attributes for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to invalidate cache for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateUserCacheAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Preview result for targeting rules.
/// </summary>
public sealed record TargetingPreview
{
    public int TotalCount { get; init; }
    public List<TargetingPreviewUser> SampleUsers { get; init; } = new();
}

/// <summary>
/// A sample user in targeting preview.
/// </summary>
public sealed record TargetingPreviewUser
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string? Location { get; init; }
    public string? AvatarUrl { get; init; }
}
