namespace CommonHall.Application.Interfaces;

/// <summary>
/// Service for managing rule-based group memberships.
/// </summary>
public interface IRuleBasedGroupService
{
    /// <summary>
    /// Recalculates the membership for a specific rule-based group.
    /// Parses the group's rule, queries matching users, and syncs memberships.
    /// </summary>
    /// <param name="groupId">The group ID to recalculate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of members after recalculation.</returns>
    Task<int> RecalculateAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates all rule-based groups.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary of group IDs to their new member counts.</returns>
    Task<Dictionary<Guid, int>> RecalculateAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users matching a rule definition without persisting changes.
    /// Useful for previewing rule-based group membership.
    /// </summary>
    /// <param name="ruleDefinition">The rule definition JSON.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching user IDs.</returns>
    Task<List<Guid>> GetMatchingUsersAsync(string ruleDefinition, CancellationToken cancellationToken = default);
}
