using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommonHall.Domain.ValueObjects;

/// <summary>
/// Represents a visibility rule for content targeting.
/// Can be serialized to/from JSON for storage in JSONB columns.
/// </summary>
public sealed record VisibilityRule
{
    public static readonly VisibilityRule Everyone = new() { Type = VisibilityRuleType.All };

    [JsonPropertyName("type")]
    public VisibilityRuleType Type { get; init; } = VisibilityRuleType.All;

    [JsonPropertyName("groupIds")]
    public List<Guid>? GroupIds { get; init; }

    [JsonPropertyName("rules")]
    public RuleSet? Rules { get; init; }

    public static VisibilityRule? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<VisibilityRule>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VisibilityRuleType
{
    All,
    Groups,
    Rules
}

public sealed record RuleSet
{
    [JsonPropertyName("logic")]
    public RuleLogic Logic { get; init; } = RuleLogic.And;

    [JsonPropertyName("conditions")]
    public List<RuleCondition> Conditions { get; init; } = new();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleLogic
{
    And,
    Or
}

public sealed record RuleCondition
{
    [JsonPropertyName("field")]
    public ConditionField Field { get; init; }

    [JsonPropertyName("operator")]
    public ConditionOperator Operator { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("values")]
    public List<string>? Values { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConditionField
{
    Department,
    Location,
    JobTitle,
    Role,
    PreferredLanguage,
    Group
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConditionOperator
{
    Equals,
    NotEquals,
    In,
    NotIn,
    Contains,
    StartsWith,
    Gte,
    Lte,
    MemberOf,
    NotMemberOf
}
