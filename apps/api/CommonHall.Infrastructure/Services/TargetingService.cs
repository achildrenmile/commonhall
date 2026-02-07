using System.Text.Json;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using CommonHall.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.Services;

/// <summary>
/// Implementation of ITargetingService with Redis caching for user attributes.
/// </summary>
public sealed class TargetingService : ITargetingService
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService? _cacheService;
    private readonly ILogger<TargetingService> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string UserAttributesCacheKeyPrefix = "targeting:user:";
    private const string UserGroupsCacheKeyPrefix = "targeting:user-groups:";

    public TargetingService(
        IApplicationDbContext context,
        ILogger<TargetingService> logger,
        ICacheService? cacheService = null)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<bool> IsVisibleAsync(
        Guid userId,
        string? visibilityRuleJson,
        CancellationToken cancellationToken = default)
    {
        // Null or empty rule means visible to all
        if (string.IsNullOrWhiteSpace(visibilityRuleJson))
            return true;

        var rule = VisibilityRule.Parse(visibilityRuleJson);
        if (rule == null)
            return true;

        return await EvaluateRuleAsync(userId, rule, cancellationToken);
    }

    public async Task<IEnumerable<T>> FilterVisibleAsync<T>(
        Guid userId,
        IEnumerable<T> items,
        Func<T, string?> ruleSelector,
        CancellationToken cancellationToken = default)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0)
            return itemList;

        // Pre-load user attributes and groups for efficient evaluation
        var userAttributes = await GetUserAttributesAsync(userId, cancellationToken);
        if (userAttributes == null)
            return Enumerable.Empty<T>();

        var userGroupIds = await GetUserGroupIdsAsync(userId, cancellationToken);

        var visibleItems = new List<T>();
        foreach (var item in itemList)
        {
            var ruleJson = ruleSelector(item);
            if (string.IsNullOrWhiteSpace(ruleJson))
            {
                visibleItems.Add(item);
                continue;
            }

            var rule = VisibilityRule.Parse(ruleJson);
            if (rule == null || await EvaluateRuleWithCachedDataAsync(rule, userAttributes, userGroupIds))
            {
                visibleItems.Add(item);
            }
        }

        return visibleItems;
    }

    public async Task<bool> EvaluateRuleAsync(
        Guid userId,
        VisibilityRule rule,
        CancellationToken cancellationToken = default)
    {
        if (rule.Type == VisibilityRuleType.All)
            return true;

        var userAttributes = await GetUserAttributesAsync(userId, cancellationToken);
        if (userAttributes == null)
            return false;

        var userGroupIds = await GetUserGroupIdsAsync(userId, cancellationToken);

        return await EvaluateRuleWithCachedDataAsync(rule, userAttributes, userGroupIds);
    }

    public async Task<TargetingPreview> GetPreviewAsync(
        string? visibilityRuleJson,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        // If no rule or "all" type, return all active users
        if (string.IsNullOrWhiteSpace(visibilityRuleJson))
        {
            var allCount = await _context.Users
                .Where(u => !u.IsDeleted && u.IsActive)
                .CountAsync(cancellationToken);

            var allSample = await _context.Users
                .Where(u => !u.IsDeleted && u.IsActive)
                .OrderBy(u => u.DisplayName)
                .Take(limit)
                .Select(u => new TargetingPreviewUser
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName,
                    Department = u.Department,
                    Location = u.Location,
                    AvatarUrl = u.AvatarUrl
                })
                .ToListAsync(cancellationToken);

            return new TargetingPreview { TotalCount = allCount, SampleUsers = allSample };
        }

        var rule = VisibilityRule.Parse(visibilityRuleJson);
        if (rule == null || rule.Type == VisibilityRuleType.All)
        {
            return await GetPreviewAsync(null, limit, cancellationToken);
        }

        // Get all active users and filter in memory
        // For production, this could be optimized with SQL generation
        var users = await _context.Users
            .Where(u => !u.IsDeleted && u.IsActive)
            .Include(u => u.GroupMemberships)
            .ToListAsync(cancellationToken);

        var matchingUsers = new List<User>();
        foreach (var user in users)
        {
            var attributes = MapToUserAttributes(user);
            var groupIds = user.GroupMemberships.Select(gm => gm.GroupId).ToHashSet();

            if (await EvaluateRuleWithCachedDataAsync(rule, attributes, groupIds))
            {
                matchingUsers.Add(user);
            }
        }

        return new TargetingPreview
        {
            TotalCount = matchingUsers.Count,
            SampleUsers = matchingUsers
                .OrderBy(u => u.DisplayName)
                .Take(limit)
                .Select(u => new TargetingPreviewUser
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName,
                    Department = u.Department,
                    Location = u.Location,
                    AvatarUrl = u.AvatarUrl
                })
                .ToList()
        };
    }

    public async Task<string> FilterWidgetsAsync(
        string contentJson,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentJson) || contentJson == "[]")
            return contentJson;

        try
        {
            using var doc = JsonDocument.Parse(contentJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return contentJson;

            var userAttributes = await GetUserAttributesAsync(userId, cancellationToken);
            if (userAttributes == null)
                return "[]";

            var userGroupIds = await GetUserGroupIdsAsync(userId, cancellationToken);

            var visibleWidgets = new List<JsonElement>();
            foreach (var widget in doc.RootElement.EnumerateArray())
            {
                // Check if widget has a visibility rule
                if (widget.TryGetProperty("visibility", out var visibilityProp))
                {
                    var visibilityJson = visibilityProp.GetRawText();
                    var rule = VisibilityRule.Parse(visibilityJson);

                    if (rule != null && !await EvaluateRuleWithCachedDataAsync(rule, userAttributes, userGroupIds))
                    {
                        continue; // Skip this widget
                    }
                }

                visibleWidgets.Add(widget);
            }

            return JsonSerializer.Serialize(visibleWidgets);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse content JSON for widget filtering");
            return contentJson;
        }
    }

    public async Task InvalidateUserCacheAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (_cacheService == null) return;

        await _cacheService.RemoveAsync($"{UserAttributesCacheKeyPrefix}{userId}", cancellationToken);
        await _cacheService.RemoveAsync($"{UserGroupsCacheKeyPrefix}{userId}", cancellationToken);
    }

    #region Private Methods

    private async Task<UserAttributes?> GetUserAttributesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{UserAttributesCacheKeyPrefix}{userId}";

        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<UserAttributes>(cacheKey, cancellationToken);
            if (cached != null)
                return cached;
        }

        var user = await _context.Users
            .Where(u => u.Id == userId && !u.IsDeleted && u.IsActive)
            .Select(u => new UserAttributes
            {
                UserId = u.Id,
                Department = u.Department,
                Location = u.Location,
                JobTitle = u.JobTitle,
                Role = u.Role.ToString(),
                PreferredLanguage = u.PreferredLanguage
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user != null && _cacheService != null)
        {
            await _cacheService.SetAsync(cacheKey, user, CacheTtl, cancellationToken);
        }

        return user;
    }

    private async Task<HashSet<Guid>> GetUserGroupIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{UserGroupsCacheKeyPrefix}{userId}";

        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<List<Guid>>(cacheKey, cancellationToken);
            if (cached != null)
                return cached.ToHashSet();
        }

        var groupIds = await _context.UserGroupMemberships
            .Where(ugm => ugm.UserId == userId)
            .Select(ugm => ugm.GroupId)
            .ToListAsync(cancellationToken);

        if (_cacheService != null)
        {
            await _cacheService.SetAsync(cacheKey, groupIds, CacheTtl, cancellationToken);
        }

        return groupIds.ToHashSet();
    }

    private Task<bool> EvaluateRuleWithCachedDataAsync(
        VisibilityRule rule,
        UserAttributes attributes,
        HashSet<Guid> groupIds)
    {
        return Task.FromResult(EvaluateRuleSync(rule, attributes, groupIds));
    }

    private bool EvaluateRuleSync(VisibilityRule rule, UserAttributes attributes, HashSet<Guid> groupIds)
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

    private bool EvaluateRuleSet(RuleSet ruleSet, UserAttributes attributes, HashSet<Guid> groupIds)
    {
        if (ruleSet.Conditions.Count == 0)
            return true;

        var results = ruleSet.Conditions.Select(c => EvaluateCondition(c, attributes, groupIds));

        return ruleSet.Logic == RuleLogic.And
            ? results.All(r => r)
            : results.Any(r => r);
    }

    private bool EvaluateCondition(RuleCondition condition, UserAttributes attributes, HashSet<Guid> groupIds)
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

            ConditionOperator.Gte =>
                CompareValues(fieldValue, condition.Value) >= 0,

            ConditionOperator.Lte =>
                CompareValues(fieldValue, condition.Value) <= 0,

            ConditionOperator.MemberOf =>
                EvaluateGroupMembership(condition, groupIds, true),

            ConditionOperator.NotMemberOf =>
                EvaluateGroupMembership(condition, groupIds, false),

            _ => true
        };
    }

    private static string? GetFieldValue(ConditionField field, UserAttributes attributes)
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

    private static int CompareValues(string? a, string? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        // Try numeric comparison first
        if (double.TryParse(a, out var aNum) && double.TryParse(b, out var bNum))
        {
            return aNum.CompareTo(bNum);
        }

        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EvaluateGroupMembership(RuleCondition condition, HashSet<Guid> groupIds, bool shouldBeMember)
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

    private static UserAttributes MapToUserAttributes(User user)
    {
        return new UserAttributes
        {
            UserId = user.Id,
            Department = user.Department,
            Location = user.Location,
            JobTitle = user.JobTitle,
            Role = user.Role.ToString(),
            PreferredLanguage = user.PreferredLanguage
        };
    }

    #endregion
}

/// <summary>
/// Cached user attributes for targeting evaluation.
/// </summary>
internal sealed record UserAttributes
{
    public Guid UserId { get; init; }
    public string? Department { get; init; }
    public string? Location { get; init; }
    public string? JobTitle { get; init; }
    public string? Role { get; init; }
    public string? PreferredLanguage { get; init; }
}
