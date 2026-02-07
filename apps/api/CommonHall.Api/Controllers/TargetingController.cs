using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[Authorize]
public class TargetingController : BaseApiController
{
    private readonly ITargetingService _targetingService;
    private readonly ICurrentUser _currentUser;

    public TargetingController(ITargetingService targetingService, ICurrentUser currentUser)
    {
        _targetingService = targetingService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Evaluate a visibility rule against the current user (for debugging).
    /// Requires Admin role.
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
        {
            return Forbid();
        }

        var userId = request.UserId ?? _currentUser.UserId ?? Guid.Empty;
        if (userId == Guid.Empty)
        {
            return BadRequest(new { error = "User ID is required" });
        }

        var isVisible = await _targetingService.IsVisibleAsync(userId, request.RuleJson, cancellationToken);

        return Ok(new { userId, isVisible, ruleJson = request.RuleJson });
    }

    /// <summary>
    /// Preview how many users match a visibility rule.
    /// Requires Admin role.
    /// </summary>
    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] string? ruleJson, [FromQuery] int limit = 5, CancellationToken cancellationToken = default)
    {
        if (_currentUser.Role < UserRole.Admin)
        {
            return Forbid();
        }

        var preview = await _targetingService.GetPreviewAsync(ruleJson, limit, cancellationToken);

        return Ok(preview);
    }

    /// <summary>
    /// Get available targeting fields and their operators.
    /// </summary>
    [HttpGet("schema")]
    public IActionResult GetSchema()
    {
        var schema = new TargetingSchema
        {
            Fields = new List<TargetingField>
            {
                new()
                {
                    Name = "department",
                    Label = "Department",
                    Operators = new[] { "equals", "not_equals", "in", "not_in", "contains", "starts_with" }
                },
                new()
                {
                    Name = "location",
                    Label = "Location",
                    Operators = new[] { "equals", "not_equals", "in", "not_in", "contains", "starts_with" }
                },
                new()
                {
                    Name = "jobTitle",
                    Label = "Job Title",
                    Operators = new[] { "equals", "not_equals", "in", "not_in", "contains", "starts_with" }
                },
                new()
                {
                    Name = "role",
                    Label = "Role",
                    Operators = new[] { "equals", "not_equals", "in", "not_in" },
                    Options = new[] { "Employee", "Editor", "Admin" }
                },
                new()
                {
                    Name = "preferredLanguage",
                    Label = "Preferred Language",
                    Operators = new[] { "equals", "not_equals", "in", "not_in" }
                },
                new()
                {
                    Name = "group",
                    Label = "Group Membership",
                    Operators = new[] { "member_of", "not_member_of" },
                    RequiresGroupSelector = true
                }
            }
        };

        return Ok(schema);
    }
}

public record EvaluateRequest
{
    public Guid? UserId { get; init; }
    public string? RuleJson { get; init; }
}

public record TargetingSchema
{
    public List<TargetingField> Fields { get; init; } = new();
}

public record TargetingField
{
    public string Name { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string[] Operators { get; init; } = Array.Empty<string>();
    public string[]? Options { get; init; }
    public bool RequiresGroupSelector { get; init; }
}
