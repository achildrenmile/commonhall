using System.Text.Json;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using CommonHall.Domain.ValueObjects;
using CommonHall.Infrastructure.Persistence;
using CommonHall.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommonHall.Tests.Unit.Services;

public class TargetingServiceTests
{
    private readonly CommonHallDbContext _context;
    private readonly TargetingService _targetingService;

    public TargetingServiceTests()
    {
        var options = new DbContextOptionsBuilder<CommonHallDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CommonHallDbContext(options);
        _targetingService = new TargetingService(
            _context,
            NullLogger<TargetingService>.Instance);
    }

    #region IsVisibleAsync Tests

    [Fact]
    public async Task IsVisibleAsync_NullRule_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId);

        // Act
        var result = await _targetingService.IsVisibleAsync(userId, null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsVisibleAsync_EmptyRule_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId);

        // Act
        var result = await _targetingService.IsVisibleAsync(userId, "");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsVisibleAsync_AllType_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId);
        var ruleJson = """{"type":"all"}""";

        // Act
        var result = await _targetingService.IsVisibleAsync(userId, ruleJson);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsVisibleAsync_UserNotFound_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid(); // User doesn't exist
        var ruleJson = """{"type":"rules","rules":{"logic":"AND","conditions":[{"field":"department","operator":"equals","value":"Engineering"}]}}""";

        // Act
        var result = await _targetingService.IsVisibleAsync(userId, ruleJson);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region EvaluateRuleAsync Tests - Operators

    [Fact]
    public async Task EvaluateRule_EqualsOperator_Matching_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, department: "Engineering");

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
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateRule_EqualsOperator_NotMatching_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, department: "Marketing");

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
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateRule_EqualsOperator_CaseInsensitive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, department: "ENGINEERING");

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.And,
                Conditions = new List<RuleCondition>
                {
                    new() { Field = ConditionField.Department, Operator = ConditionOperator.Equals, Value = "engineering" }
                }
            }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateRule_NotEqualsOperator_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, department: "Marketing");

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.And,
                Conditions = new List<RuleCondition>
                {
                    new() { Field = ConditionField.Department, Operator = ConditionOperator.NotEquals, Value = "Engineering" }
                }
            }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateRule_InOperator_Matching_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, location: "New York");

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.And,
                Conditions = new List<RuleCondition>
                {
                    new()
                    {
                        Field = ConditionField.Location,
                        Operator = ConditionOperator.In,
                        Values = new List<string> { "New York", "Los Angeles", "Chicago" }
                    }
                }
            }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateRule_InOperator_NotMatching_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, location: "Boston");

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.And,
                Conditions = new List<RuleCondition>
                {
                    new()
                    {
                        Field = ConditionField.Location,
                        Operator = ConditionOperator.In,
                        Values = new List<string> { "New York", "Los Angeles", "Chicago" }
                    }
                }
            }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateRule_NotInOperator_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, location: "Boston");

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.And,
                Conditions = new List<RuleCondition>
                {
                    new()
                    {
                        Field = ConditionField.Location,
                        Operator = ConditionOperator.NotIn,
                        Values = new List<string> { "New York", "Los Angeles", "Chicago" }
                    }
                }
            }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateRule_ContainsOperator_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, jobTitle: "Senior Software Engineer");

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.And,
                Conditions = new List<RuleCondition>
                {
                    new() { Field = ConditionField.JobTitle, Operator = ConditionOperator.Contains, Value = "Engineer" }
                }
            }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateRule_StartsWithOperator_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, jobTitle: "Senior Software Engineer");

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.And,
                Conditions = new List<RuleCondition>
                {
                    new() { Field = ConditionField.JobTitle, Operator = ConditionOperator.StartsWith, Value = "Senior" }
                }
            }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region EvaluateRuleAsync Tests - Logic

    [Fact]
    public async Task EvaluateRule_AndLogic_AllConditionsMatch_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, department: "Engineering", location: "New York");

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.And,
                Conditions = new List<RuleCondition>
                {
                    new() { Field = ConditionField.Department, Operator = ConditionOperator.Equals, Value = "Engineering" },
                    new() { Field = ConditionField.Location, Operator = ConditionOperator.Equals, Value = "New York" }
                }
            }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateRule_AndLogic_OneConditionFails_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, department: "Engineering", location: "Boston");

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.And,
                Conditions = new List<RuleCondition>
                {
                    new() { Field = ConditionField.Department, Operator = ConditionOperator.Equals, Value = "Engineering" },
                    new() { Field = ConditionField.Location, Operator = ConditionOperator.Equals, Value = "New York" }
                }
            }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateRule_OrLogic_OneConditionMatches_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, department: "Marketing", location: "New York");

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.Or,
                Conditions = new List<RuleCondition>
                {
                    new() { Field = ConditionField.Department, Operator = ConditionOperator.Equals, Value = "Engineering" },
                    new() { Field = ConditionField.Location, Operator = ConditionOperator.Equals, Value = "New York" }
                }
            }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateRule_OrLogic_NoConditionsMatch_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, department: "Marketing", location: "Boston");

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.Or,
                Conditions = new List<RuleCondition>
                {
                    new() { Field = ConditionField.Department, Operator = ConditionOperator.Equals, Value = "Engineering" },
                    new() { Field = ConditionField.Location, Operator = ConditionOperator.Equals, Value = "New York" }
                }
            }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Group Membership Tests

    [Fact]
    public async Task EvaluateRule_GroupsType_UserInGroup_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        await CreateTestUserWithGroup(userId, groupId);

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Groups,
            GroupIds = new List<Guid> { groupId }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateRule_GroupsType_UserNotInGroup_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var otherGroupId = Guid.NewGuid();
        await CreateTestUserWithGroup(userId, otherGroupId);

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Groups,
            GroupIds = new List<Guid> { groupId }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateRule_MemberOfOperator_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        await CreateTestUserWithGroup(userId, groupId);

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.And,
                Conditions = new List<RuleCondition>
                {
                    new() { Field = ConditionField.Group, Operator = ConditionOperator.MemberOf, Value = groupId.ToString() }
                }
            }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateRule_NotMemberOfOperator_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var otherGroupId = Guid.NewGuid();
        await CreateTestUserWithGroup(userId, otherGroupId);

        var rule = new VisibilityRule
        {
            Type = VisibilityRuleType.Rules,
            Rules = new RuleSet
            {
                Logic = RuleLogic.And,
                Conditions = new List<RuleCondition>
                {
                    new() { Field = ConditionField.Group, Operator = ConditionOperator.NotMemberOf, Value = groupId.ToString() }
                }
            }
        };

        // Act
        var result = await _targetingService.EvaluateRuleAsync(userId, rule);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region FilterVisibleAsync Tests

    [Fact]
    public async Task FilterVisibleAsync_ShouldReturnOnlyVisibleItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, department: "Engineering");

        var items = new List<TestItem>
        {
            new() { Id = 1, VisibilityRule = null },
            new() { Id = 2, VisibilityRule = """{"type":"all"}""" },
            new() { Id = 3, VisibilityRule = """{"type":"rules","rules":{"logic":"AND","conditions":[{"field":"department","operator":"equals","value":"Engineering"}]}}""" },
            new() { Id = 4, VisibilityRule = """{"type":"rules","rules":{"logic":"AND","conditions":[{"field":"department","operator":"equals","value":"Marketing"}]}}""" }
        };

        // Act
        var result = await _targetingService.FilterVisibleAsync(userId, items, i => i.VisibilityRule);

        // Assert
        result.Should().HaveCount(3);
        result.Select(i => i.Id).Should().Contain(new[] { 1, 2, 3 });
        result.Select(i => i.Id).Should().NotContain(4);
    }

    #endregion

    #region FilterWidgetsAsync Tests

    [Fact]
    public async Task FilterWidgetsAsync_EmptyContent_ShouldReturnAsIs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId);

        // Act
        var result = await _targetingService.FilterWidgetsAsync("[]", userId);

        // Assert
        result.Should().Be("[]");
    }

    [Fact]
    public async Task FilterWidgetsAsync_NoVisibilityRules_ShouldReturnAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId);
        var content = """[{"id":"1","type":"rich-text","data":{"html":"Hello"}},{"id":"2","type":"button","data":{"text":"Click"}}]""";

        // Act
        var result = await _targetingService.FilterWidgetsAsync(content, userId);

        // Assert
        var widgets = JsonSerializer.Deserialize<JsonElement[]>(result);
        widgets.Should().HaveCount(2);
    }

    [Fact]
    public async Task FilterWidgetsAsync_WithVisibilityRule_ShouldFilterNonVisible()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUser(userId, department: "Engineering");
        var content = """[{"id":"1","type":"rich-text","data":{"html":"Hello"}},{"id":"2","type":"button","data":{"text":"Click"},"visibility":{"type":"rules","rules":{"logic":"AND","conditions":[{"field":"department","operator":"equals","value":"Marketing"}]}}}]""";

        // Act
        var result = await _targetingService.FilterWidgetsAsync(content, userId);

        // Assert
        var widgets = JsonSerializer.Deserialize<JsonElement[]>(result);
        widgets.Should().HaveCount(1);
    }

    #endregion

    #region Helper Methods

    private async Task CreateTestUser(
        Guid userId,
        string? department = null,
        string? location = null,
        string? jobTitle = null,
        string? preferredLanguage = null)
    {
        var user = new User
        {
            Id = userId,
            UserName = $"user{userId:N}@test.com",
            Email = $"user{userId:N}@test.com",
            DisplayName = "Test User",
            FirstName = "Test",
            LastName = "User",
            Department = department,
            Location = location,
            JobTitle = jobTitle,
            PreferredLanguage = preferredLanguage,
            Role = UserRole.Employee,
            IsActive = true,
            IsDeleted = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    private async Task CreateTestUserWithGroup(Guid userId, Guid groupId)
    {
        await CreateTestUser(userId);

        var group = new UserGroup
        {
            Id = groupId,
            Name = "Test Group",
            Slug = "test-group",
            GroupType = GroupType.Manual,
            IsActive = true
        };
        _context.UserGroups.Add(group);

        var membership = new UserGroupMembership
        {
            UserId = userId,
            GroupId = groupId
        };
        _context.UserGroupMemberships.Add(membership);

        await _context.SaveChangesAsync();
    }

    private sealed class TestItem
    {
        public int Id { get; init; }
        public string? VisibilityRule { get; init; }
    }

    #endregion
}
