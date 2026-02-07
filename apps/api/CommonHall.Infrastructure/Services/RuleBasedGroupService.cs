using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using CommonHall.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.Services;

/// <summary>
/// Service for managing rule-based group memberships.
/// </summary>
public sealed class RuleBasedGroupService : IRuleBasedGroupService
{
    private readonly IApplicationDbContext _context;
    private readonly ITargetingService _targetingService;
    private readonly ILogger<RuleBasedGroupService> _logger;

    public RuleBasedGroupService(
        IApplicationDbContext context,
        ITargetingService targetingService,
        ILogger<RuleBasedGroupService> logger)
    {
        _context = context;
        _targetingService = targetingService;
        _logger = logger;
    }

    public async Task<int> RecalculateAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await _context.UserGroups
            .FirstOrDefaultAsync(g => g.Id == groupId && g.Type == GroupType.RuleBased, cancellationToken);

        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found or is not rule-based", groupId);
            return 0;
        }

        if (string.IsNullOrWhiteSpace(group.RuleDefinition))
        {
            _logger.LogWarning("Group {GroupId} has no rule definition", groupId);
            return 0;
        }

        var matchingUserIds = await GetMatchingUsersAsync(group.RuleDefinition, cancellationToken);

        // Get current memberships
        var currentMemberships = await _context.UserGroupMemberships
            .Where(ugm => ugm.UserGroupId == groupId)
            .ToListAsync(cancellationToken);

        var currentMemberIds = currentMemberships.Select(m => m.UserId).ToHashSet();
        var newMemberIds = matchingUserIds.ToHashSet();

        // Determine changes
        var toAdd = newMemberIds.Except(currentMemberIds).ToList();
        var toRemove = currentMemberIds.Except(newMemberIds).ToList();

        // Remove members who no longer match
        if (toRemove.Count > 0)
        {
            var membershipsToRemove = currentMemberships.Where(m => toRemove.Contains(m.UserId));
            foreach (var membership in membershipsToRemove)
            {
                _context.UserGroupMemberships.Remove(membership);
            }
        }

        // Add new matching members
        foreach (var userId in toAdd)
        {
            _context.UserGroupMemberships.Add(new UserGroupMembership
            {
                UserGroupId = groupId,
                UserId = userId,
                JoinedAt = DateTimeOffset.UtcNow
            });
        }

        if (toAdd.Count > 0 || toRemove.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Recalculated group {GroupId}: added {Added}, removed {Removed}, total {Total}",
                groupId, toAdd.Count, toRemove.Count, newMemberIds.Count);

            // Invalidate cache for affected users
            foreach (var userId in toAdd.Concat(toRemove))
            {
                await _targetingService.InvalidateUserCacheAsync(userId, cancellationToken);
            }
        }

        return newMemberIds.Count;
    }

    public async Task<Dictionary<Guid, int>> RecalculateAllAsync(CancellationToken cancellationToken = default)
    {
        var ruleBasedGroups = await _context.UserGroups
            .Where(g => g.Type == GroupType.RuleBased && !string.IsNullOrEmpty(g.RuleDefinition))
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);

        var results = new Dictionary<Guid, int>();

        foreach (var groupId in ruleBasedGroups)
        {
            try
            {
                var count = await RecalculateAsync(groupId, cancellationToken);
                results[groupId] = count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recalculate group {GroupId}", groupId);
                results[groupId] = -1; // Indicate error
            }
        }

        return results;
    }

    public async Task<List<Guid>> GetMatchingUsersAsync(
        string ruleDefinition,
        CancellationToken cancellationToken = default)
    {
        var rule = VisibilityRule.Parse(ruleDefinition);
        if (rule == null)
        {
            _logger.LogWarning("Failed to parse rule definition: {Rule}", ruleDefinition);
            return new List<Guid>();
        }

        // Get all active users
        var users = await _context.Users
            .Where(u => !u.IsDeleted && u.IsActive)
            .Include(u => u.GroupMemberships)
            .ToListAsync(cancellationToken);

        var matchingUsers = new List<Guid>();

        foreach (var user in users)
        {
            var userAttributes = new UserAttributesForEvaluation
            {
                Department = user.Department,
                Location = user.Location,
                JobTitle = user.JobTitle,
                Role = user.Role.ToString(),
                PreferredLanguage = user.PreferredLanguage
            };

            var groupIds = user.GroupMemberships.Select(gm => gm.UserGroupId).ToHashSet();

            if (EvaluateRule(rule, userAttributes, groupIds))
            {
                matchingUsers.Add(user.Id);
            }
        }

        return matchingUsers;
    }

    private static bool EvaluateRule(
        VisibilityRule rule,
        UserAttributesForEvaluation attributes,
        HashSet<Guid> groupIds)
    {
        switch (rule.Type)
        {
            case VisibilityRuleType.All:
                return true;

            case VisibilityRuleType.Groups:
                if (rule.GroupIds == null || rule.GroupIds.Count == 0)
                    return true;
                return rule.GroupIds.Any(gid => groupIds.Contains(gid));

            case VisibilityRuleType.Rules:
                if (rule.Rules == null || rule.Rules.Conditions.Count == 0)
                    return true;
                return EvaluateRuleSet(rule.Rules, attributes, groupIds);

            default:
                return true;
        }
    }

    private static bool EvaluateRuleSet(
        RuleSet ruleSet,
        UserAttributesForEvaluation attributes,
        HashSet<Guid> groupIds)
    {
        if (ruleSet.Conditions.Count == 0)
            return true;

        var results = ruleSet.Conditions.Select(c => EvaluateCondition(c, attributes, groupIds));

        return ruleSet.Logic == RuleLogic.And
            ? results.All(r => r)
            : results.Any(r => r);
    }

    private static bool EvaluateCondition(
        RuleCondition condition,
        UserAttributesForEvaluation attributes,
        HashSet<Guid> groupIds)
    {
        var fieldValue = GetFieldValue(condition.Field, attributes);

        return condition.Operator switch
        {
            ConditionOperator.Equals =>
                string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),

            ConditionOperator.NotEquals =>
                !string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),

            ConditionOperator.In =>
                condition.Values?.Any(v => string.Equals(fieldValue, v, StringComparison.OrdinalIgnoreCase)) ?? false,

            ConditionOperator.NotIn =>
                !(condition.Values?.Any(v => string.Equals(fieldValue, v, StringComparison.OrdinalIgnoreCase)) ?? false),

            ConditionOperator.Contains =>
                fieldValue?.Contains(condition.Value ?? "", StringComparison.OrdinalIgnoreCase) ?? false,

            ConditionOperator.StartsWith =>
                fieldValue?.StartsWith(condition.Value ?? "", StringComparison.OrdinalIgnoreCase) ?? false,

            ConditionOperator.MemberOf =>
                EvaluateGroupMembership(condition, groupIds, true),

            ConditionOperator.NotMemberOf =>
                EvaluateGroupMembership(condition, groupIds, false),

            _ => true
        };
    }

    private static string? GetFieldValue(ConditionField field, UserAttributesForEvaluation attributes)
    {
        return field switch
        {
            ConditionField.Department => attributes.Department,
            ConditionField.Location => attributes.Location,
            ConditionField.JobTitle => attributes.JobTitle,
            ConditionField.Role => attributes.Role,
            ConditionField.PreferredLanguage => attributes.PreferredLanguage,
            _ => null
        };
    }

    private static bool EvaluateGroupMembership(
        RuleCondition condition,
        HashSet<Guid> groupIds,
        bool shouldBeMember)
    {
        var targetGroupIds = new List<Guid>();

        if (condition.Value != null && Guid.TryParse(condition.Value, out var singleId))
        {
            targetGroupIds.Add(singleId);
        }

        if (condition.Values != null)
        {
            targetGroupIds.AddRange(
                condition.Values
                    .Where(v => Guid.TryParse(v, out _))
                    .Select(v => Guid.Parse(v)));
        }

        if (targetGroupIds.Count == 0)
            return true;

        var isMember = targetGroupIds.Any(gid => groupIds.Contains(gid));
        return shouldBeMember ? isMember : !isMember;
    }

    private sealed record UserAttributesForEvaluation
    {
        public string? Department { get; init; }
        public string? Location { get; init; }
        public string? JobTitle { get; init; }
        public string? Role { get; init; }
        public string? PreferredLanguage { get; init; }
    }
}
