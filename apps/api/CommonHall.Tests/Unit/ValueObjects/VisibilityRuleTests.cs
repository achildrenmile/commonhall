using CommonHall.Domain.ValueObjects;
using FluentAssertions;

namespace CommonHall.Tests.Unit.ValueObjects;

public class VisibilityRuleTests
{
    #region Parse Tests

    [Fact]
    public void Parse_NullJson_ShouldReturnNull()
    {
        // Act
        var result = VisibilityRule.Parse(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_EmptyJson_ShouldReturnNull()
    {
        // Act
        var result = VisibilityRule.Parse("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_InvalidJson_ShouldReturnNull()
    {
        // Act
        var result = VisibilityRule.Parse("not valid json");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_TypeAll_ShouldParseCorrectly()
    {
        // Arrange
        var json = """{"type":"all"}""";

        // Act
        var result = VisibilityRule.Parse(json);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(VisibilityRuleType.All);
    }

    [Fact]
    public void Parse_TypeGroups_ShouldParseCorrectly()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var json = $$$"""{"type":"groups","groupIds":["{{{groupId}}}"]}""";

        // Act
        var result = VisibilityRule.Parse(json);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(VisibilityRuleType.Groups);
        result.GroupIds.Should().Contain(groupId);
    }

    [Fact]
    public void Parse_TypeRules_ShouldParseCorrectly()
    {
        // Arrange
        var json = """{"type":"rules","rules":{"logic":"AND","conditions":[{"field":"department","operator":"equals","value":"Engineering"}]}}""";

        // Act
        var result = VisibilityRule.Parse(json);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(VisibilityRuleType.Rules);
        result.Rules.Should().NotBeNull();
        result.Rules!.Logic.Should().Be(RuleLogic.And);
        result.Rules.Conditions.Should().HaveCount(1);
        result.Rules.Conditions[0].Field.Should().Be(ConditionField.Department);
        result.Rules.Conditions[0].Operator.Should().Be(ConditionOperator.Equals);
        result.Rules.Conditions[0].Value.Should().Be("Engineering");
    }

    [Fact]
    public void Parse_OrLogic_ShouldParseCorrectly()
    {
        // Arrange
        var json = """{"type":"rules","rules":{"logic":"OR","conditions":[{"field":"location","operator":"in","values":["NYC","LA"]}]}}""";

        // Act
        var result = VisibilityRule.Parse(json);

        // Assert
        result.Should().NotBeNull();
        result!.Rules!.Logic.Should().Be(RuleLogic.Or);
        result.Rules.Conditions[0].Operator.Should().Be(ConditionOperator.In);
        result.Rules.Conditions[0].Values.Should().Contain(new[] { "NYC", "LA" });
    }

    [Fact]
    public void Parse_AllOperators_ShouldParseCorrectly()
    {
        // Test each operator
        var operators = new Dictionary<string, ConditionOperator>
        {
            { "equals", ConditionOperator.Equals },
            { "not_equals", ConditionOperator.NotEquals },
            { "in", ConditionOperator.In },
            { "not_in", ConditionOperator.NotIn },
            { "contains", ConditionOperator.Contains },
            { "starts_with", ConditionOperator.StartsWith },
            { "gte", ConditionOperator.Gte },
            { "lte", ConditionOperator.Lte },
            { "member_of", ConditionOperator.MemberOf },
            { "not_member_of", ConditionOperator.NotMemberOf }
        };

        foreach (var (jsonOp, expectedOp) in operators)
        {
            var json = $$$"""{"type":"rules","rules":{"logic":"AND","conditions":[{"field":"department","operator":"{{{jsonOp}}}","value":"test"}]}}""";
            var result = VisibilityRule.Parse(json);

            result.Should().NotBeNull($"operator {jsonOp} should parse");
            result!.Rules!.Conditions[0].Operator.Should().Be(expectedOp);
        }
    }

    [Fact]
    public void Parse_AllFields_ShouldParseCorrectly()
    {
        // Test each field
        var fields = new Dictionary<string, ConditionField>
        {
            { "department", ConditionField.Department },
            { "location", ConditionField.Location },
            { "jobTitle", ConditionField.JobTitle },
            { "role", ConditionField.Role },
            { "preferredLanguage", ConditionField.PreferredLanguage },
            { "group", ConditionField.Group }
        };

        foreach (var (jsonField, expectedField) in fields)
        {
            var json = $$$"""{"type":"rules","rules":{"logic":"AND","conditions":[{"field":"{{{jsonField}}}","operator":"equals","value":"test"}]}}""";
            var result = VisibilityRule.Parse(json);

            result.Should().NotBeNull($"field {jsonField} should parse");
            result!.Rules!.Conditions[0].Field.Should().Be(expectedField);
        }
    }

    #endregion

    #region ToJson Tests

    [Fact]
    public void ToJson_AllType_ShouldSerializeCorrectly()
    {
        // Arrange
        var rule = VisibilityRule.Everyone;

        // Act
        var json = rule.ToJson();

        // Assert
        json.Should().Contain("\"type\":\"all\"");
    }

    [Fact]
    public void ToJson_GroupsType_ShouldSerializeCorrectly()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Groups,
            GroupIds = new List<Guid> { groupId }
        };

        // Act
        var json = rule.ToJson();

        // Assert
        json.Should().Contain("\"type\":\"groups\"");
        json.Should().Contain(groupId.ToString());
    }

    [Fact]
    public void ToJson_RulesType_ShouldSerializeCorrectly()
    {
        // Arrange
        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.And,
                Conditions = new List<RuleCondition>
                {
                    new() { Field = ConditionField.Department, Operator = ConditionOperator.Equals, Value = "Engineering" }
                }
            }
        };

        // Act
        var json = rule.ToJson();

        // Assert
        json.Should().Contain("\"type\":\"rules\"");
        json.Should().Contain("\"logic\":\"AND\"");
        json.Should().Contain("\"field\":\"department\"");
        json.Should().Contain("\"operator\":\"equals\"");
        json.Should().Contain("\"value\":\"Engineering\"");
    }

    [Fact]
    public void RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var originalRule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.Or,
                Conditions = new List<RuleCondition>
                {
                    new() { Field = ConditionField.Department, Operator = ConditionOperator.Equals, Value = "Engineering" },
                    new() { Field = ConditionField.Location, Operator = ConditionOperator.In, Values = new List<string> { "NYC", "LA" } }
                }
            }
        };

        // Act
        var json = originalRule.ToJson();
        var parsedRule = VisibilityRule.Parse(json);

        // Assert
        parsedRule.Should().NotBeNull();
        parsedRule!.Type.Should().Be(originalRule.Type);
        parsedRule.Rules!.Logic.Should().Be(originalRule.Rules.Logic);
        parsedRule.Rules.Conditions.Should().HaveCount(2);
        parsedRule.Rules.Conditions[0].Field.Should().Be(ConditionField.Department);
        parsedRule.Rules.Conditions[0].Value.Should().Be("Engineering");
        parsedRule.Rules.Conditions[1].Field.Should().Be(ConditionField.Location);
        parsedRule.Rules.Conditions[1].Values.Should().Contain(new[] { "NYC", "LA" });
    }

    #endregion

    #region Everyone Constant Tests

    [Fact]
    public void Everyone_ShouldBeAllType()
    {
        // Assert
        VisibilityRule.Everyone.Type.Should().Be(VisibilityRuleType.All);
        VisibilityRule.Everyone.GroupIds.Should().BeNull();
        VisibilityRule.Everyone.Rules.Should().BeNull();
    }

    #endregion
}
