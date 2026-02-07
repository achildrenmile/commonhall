using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Api.Controllers;

[Authorize]
public class SurveysController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public SurveysController(
        IApplicationDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all surveys with optional filtering.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] SurveyStatus? status,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var query = _context.Surveys
            .Include(s => s.Questions)
            .Include(s => s.Responses)
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        var surveys = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SurveyListDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Type = s.Type,
                IsAnonymous = s.IsAnonymous,
                Status = s.Status,
                StartsAt = s.StartsAt,
                EndsAt = s.EndsAt,
                QuestionCount = s.Questions.Count,
                ResponseCount = s.Responses.Count(r => r.IsComplete),
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(surveys);
    }

    /// <summary>
    /// Get a single survey by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var survey = await _context.Surveys
            .Include(s => s.Questions)
            .Include(s => s.Space)
            .Where(s => s.Id == id && !s.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (survey == null)
            return NotFound();

        var responseCount = await _context.SurveyResponses
            .CountAsync(r => r.SurveyId == id && r.IsComplete, cancellationToken);

        return Ok(new SurveyDetailDto
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            Type = survey.Type,
            RecurrenceConfig = survey.RecurrenceConfig,
            IsAnonymous = survey.IsAnonymous,
            Status = survey.Status,
            StartsAt = survey.StartsAt,
            EndsAt = survey.EndsAt,
            TargetGroupIds = survey.TargetGroupIds,
            SpaceId = survey.SpaceId,
            SpaceName = survey.Space?.Name,
            Questions = survey.Questions
                .OrderBy(q => q.SortOrder)
                .Select(q => new SurveyQuestionDto
                {
                    Id = q.Id,
                    Type = q.Type,
                    QuestionText = q.QuestionText,
                    Description = q.Description,
                    Options = q.Options,
                    IsRequired = q.IsRequired,
                    SortOrder = q.SortOrder,
                    Settings = q.Settings
                })
                .ToList(),
            ResponseCount = responseCount,
            CreatedAt = survey.CreatedAt,
            UpdatedAt = survey.UpdatedAt
        });
    }

    /// <summary>
    /// Create a new survey.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSurveyRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var survey = new Survey
        {
            Title = request.Title,
            Description = request.Description,
            Type = request.Type ?? SurveyType.OneTime,
            RecurrenceConfig = request.RecurrenceConfig,
            IsAnonymous = request.IsAnonymous ?? false,
            Status = SurveyStatus.Draft,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            TargetGroupIds = request.TargetGroupIds,
            SpaceId = request.SpaceId
        };

        _context.Surveys.Add(survey);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = survey.Id }, new SurveyDetailDto
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            Type = survey.Type,
            RecurrenceConfig = survey.RecurrenceConfig,
            IsAnonymous = survey.IsAnonymous,
            Status = survey.Status,
            StartsAt = survey.StartsAt,
            EndsAt = survey.EndsAt,
            TargetGroupIds = survey.TargetGroupIds,
            SpaceId = survey.SpaceId,
            Questions = new List<SurveyQuestionDto>(),
            CreatedAt = survey.CreatedAt,
            UpdatedAt = survey.UpdatedAt
        });
    }

    /// <summary>
    /// Update a survey.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSurveyRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var survey = await _context.Surveys
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (survey == null)
            return NotFound();

        if (survey.Status != SurveyStatus.Draft)
            return BadRequest(new { error = "Only draft surveys can be modified" });

        if (request.Title != null) survey.Title = request.Title;
        if (request.Description != null) survey.Description = request.Description;
        if (request.Type.HasValue) survey.Type = request.Type.Value;
        if (request.RecurrenceConfig != null) survey.RecurrenceConfig = request.RecurrenceConfig;
        if (request.IsAnonymous.HasValue) survey.IsAnonymous = request.IsAnonymous.Value;
        if (request.StartsAt.HasValue) survey.StartsAt = request.StartsAt;
        if (request.EndsAt.HasValue) survey.EndsAt = request.EndsAt;
        if (request.TargetGroupIds != null) survey.TargetGroupIds = request.TargetGroupIds;
        if (request.SpaceId.HasValue) survey.SpaceId = request.SpaceId == Guid.Empty ? null : request.SpaceId;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Survey updated" });
    }

    /// <summary>
    /// Update survey questions (bulk).
    /// </summary>
    [HttpPut("{id:guid}/questions")]
    public async Task<IActionResult> UpdateQuestions(
        Guid id,
        [FromBody] UpdateQuestionsRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var survey = await _context.Surveys
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (survey == null)
            return NotFound();

        if (survey.Status != SurveyStatus.Draft)
            return BadRequest(new { error = "Only draft surveys can be modified" });

        // Remove questions not in the list
        var newIds = request.Questions.Where(q => q.Id.HasValue).Select(q => q.Id!.Value).ToHashSet();
        var toRemove = survey.Questions.Where(q => !newIds.Contains(q.Id)).ToList();
        foreach (var q in toRemove)
        {
            _context.SurveyQuestions.Remove(q);
        }

        // Update/add questions
        for (int i = 0; i < request.Questions.Count; i++)
        {
            var input = request.Questions[i];

            if (input.Id.HasValue)
            {
                var existing = survey.Questions.FirstOrDefault(q => q.Id == input.Id.Value);
                if (existing != null)
                {
                    existing.SortOrder = i;
                    existing.Type = input.Type;
                    existing.QuestionText = input.QuestionText;
                    existing.Description = input.Description;
                    existing.Options = input.Options;
                    existing.IsRequired = input.IsRequired ?? true;
                    existing.Settings = input.Settings;
                }
            }
            else
            {
                var newQ = new SurveyQuestion
                {
                    SurveyId = id,
                    SortOrder = i,
                    Type = input.Type,
                    QuestionText = input.QuestionText,
                    Description = input.Description,
                    Options = input.Options,
                    IsRequired = input.IsRequired ?? true,
                    Settings = input.Settings
                };
                _context.SurveyQuestions.Add(newQ);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Questions updated" });
    }

    /// <summary>
    /// Activate a survey.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var survey = await _context.Surveys
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (survey == null)
            return NotFound();

        if (survey.Questions.Count == 0)
            return BadRequest(new { error = "Survey must have at least one question" });

        survey.Status = SurveyStatus.Active;
        if (!survey.StartsAt.HasValue)
            survey.StartsAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Survey activated" });
    }

    /// <summary>
    /// Close a survey.
    /// </summary>
    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var survey = await _context.Surveys
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (survey == null)
            return NotFound();

        survey.Status = SurveyStatus.Closed;
        survey.EndsAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Survey closed" });
    }

    /// <summary>
    /// Submit a survey response.
    /// </summary>
    [HttpPost("{id:guid}/respond")]
    public async Task<IActionResult> Respond(
        Guid id,
        [FromBody] SubmitResponseRequest request,
        CancellationToken cancellationToken)
    {
        var survey = await _context.Surveys
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (survey == null)
            return NotFound();

        if (survey.Status != SurveyStatus.Active)
            return BadRequest(new { error = "Survey is not active" });

        if (survey.EndsAt.HasValue && survey.EndsAt < DateTimeOffset.UtcNow)
            return BadRequest(new { error = "Survey has ended" });

        // Check for existing response
        string? userHash = null;
        SurveyResponse? existingResponse = null;

        if (survey.IsAnonymous)
        {
            userHash = ComputeUserHash(_currentUser.UserId!.Value, id);
            existingResponse = await _context.SurveyResponses
                .FirstOrDefaultAsync(r => r.SurveyId == id && r.UserHash == userHash, cancellationToken);
        }
        else
        {
            existingResponse = await _context.SurveyResponses
                .FirstOrDefaultAsync(r => r.SurveyId == id && r.UserId == _currentUser.UserId, cancellationToken);
        }

        if (existingResponse != null && existingResponse.IsComplete)
            return BadRequest(new { error = "You have already completed this survey" });

        // Create or update response
        var response = existingResponse ?? new SurveyResponse
        {
            SurveyId = id,
            UserId = survey.IsAnonymous ? null : _currentUser.UserId,
            UserHash = userHash
        };

        if (existingResponse == null)
        {
            _context.SurveyResponses.Add(response);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Validate and save answers
        foreach (var question in survey.Questions)
        {
            var answerValue = request.Answers.TryGetValue(question.Id.ToString(), out var val) ? val : null;

            if (question.IsRequired && answerValue == null)
                return BadRequest(new { error = $"Question '{question.QuestionText}' is required" });

            if (answerValue != null)
            {
                var existingAnswer = await _context.SurveyAnswers
                    .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == question.Id, cancellationToken);

                if (existingAnswer != null)
                {
                    existingAnswer.Value = JsonSerializer.Serialize(answerValue);
                }
                else
                {
                    var answer = new SurveyAnswer
                    {
                        ResponseId = response.Id,
                        QuestionId = question.Id,
                        Value = JsonSerializer.Serialize(answerValue)
                    };
                    _context.SurveyAnswers.Add(answer);
                }
            }
        }

        response.IsComplete = true;
        response.CompletedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Response submitted", responseId = response.Id });
    }

    /// <summary>
    /// Get current user's response to a survey.
    /// </summary>
    [HttpGet("{id:guid}/my-response")]
    public async Task<IActionResult> GetMyResponse(Guid id, CancellationToken cancellationToken)
    {
        var survey = await _context.Surveys
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (survey == null)
            return NotFound();

        SurveyResponse? response;

        if (survey.IsAnonymous)
        {
            var userHash = ComputeUserHash(_currentUser.UserId!.Value, id);
            response = await _context.SurveyResponses
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.SurveyId == id && r.UserHash == userHash, cancellationToken);
        }
        else
        {
            response = await _context.SurveyResponses
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.SurveyId == id && r.UserId == _currentUser.UserId, cancellationToken);
        }

        if (response == null)
            return Ok(new { hasResponse = false });

        return Ok(new
        {
            hasResponse = true,
            isComplete = response.IsComplete,
            completedAt = response.CompletedAt,
            answers = response.Answers.ToDictionary(
                a => a.QuestionId.ToString(),
                a => JsonSerializer.Deserialize<object>(a.Value))
        });
    }

    /// <summary>
    /// Get survey analytics.
    /// </summary>
    [HttpGet("{id:guid}/analytics")]
    public async Task<IActionResult> GetAnalytics(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var survey = await _context.Surveys
            .Include(s => s.Questions)
            .Include(s => s.Responses)
            .ThenInclude(r => r.Answers)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (survey == null)
            return NotFound();

        var completeResponses = survey.Responses.Where(r => r.IsComplete).ToList();
        var totalResponses = completeResponses.Count;

        var questionAnalytics = survey.Questions
            .OrderBy(q => q.SortOrder)
            .Select(q =>
            {
                var answers = completeResponses
                    .SelectMany(r => r.Answers)
                    .Where(a => a.QuestionId == q.Id)
                    .ToList();

                return new QuestionAnalyticsDto
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    Type = q.Type,
                    TotalAnswers = answers.Count,
                    Analytics = BuildQuestionAnalytics(q, answers)
                };
            })
            .ToList();

        return Ok(new SurveyAnalyticsDto
        {
            SurveyId = id,
            TotalResponses = totalResponses,
            CompleteResponses = totalResponses,
            ResponseRate = 0, // Would need target population to calculate
            QuestionAnalytics = questionAnalytics
        });
    }

    /// <summary>
    /// Export survey responses as CSV.
    /// </summary>
    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> ExportCsv(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var survey = await _context.Surveys
            .Include(s => s.Questions)
            .Include(s => s.Responses)
            .ThenInclude(r => r.Answers)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (survey == null)
            return NotFound();

        var sb = new StringBuilder();
        var questions = survey.Questions.OrderBy(q => q.SortOrder).ToList();

        // Header
        var headers = new List<string> { "Response ID", "Completed At" };
        if (!survey.IsAnonymous)
            headers.Add("User ID");
        headers.AddRange(questions.Select(q => q.QuestionText));
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        // Data rows
        foreach (var response in survey.Responses.Where(r => r.IsComplete))
        {
            var row = new List<string>
            {
                response.Id.ToString(),
                response.CompletedAt?.ToString("O") ?? ""
            };

            if (!survey.IsAnonymous)
                row.Add(response.UserId?.ToString() ?? "");

            foreach (var question in questions)
            {
                var answer = response.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                row.Add(answer != null ? FormatAnswerForCsv(answer.Value) : "");
            }

            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"survey-{id}-responses.csv");
    }

    /// <summary>
    /// Delete a survey.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var survey = await _context.Surveys
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (survey == null)
            return NotFound();

        survey.IsDeleted = true;
        survey.DeletedAt = DateTimeOffset.UtcNow;
        survey.DeletedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static string ComputeUserHash(Guid userId, Guid surveyId)
    {
        var input = $"{userId}:{surveyId}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    private static object BuildQuestionAnalytics(SurveyQuestion question, List<SurveyAnswer> answers)
    {
        switch (question.Type)
        {
            case SurveyQuestionType.SingleChoice:
            case SurveyQuestionType.MultiChoice:
            case SurveyQuestionType.YesNo:
                var optionCounts = new Dictionary<string, int>();
                foreach (var answer in answers)
                {
                    var values = ParseAnswerValues(answer.Value);
                    foreach (var v in values)
                    {
                        var key = v?.ToString() ?? "";
                        optionCounts[key] = optionCounts.GetValueOrDefault(key, 0) + 1;
                    }
                }
                return new { options = optionCounts };

            case SurveyQuestionType.Rating:
            case SurveyQuestionType.NPS:
                var ratings = answers
                    .Select(a => ParseNumericValue(a.Value))
                    .Where(v => v.HasValue)
                    .Select(v => v!.Value)
                    .ToList();

                var distribution = ratings.GroupBy(r => r)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new
                {
                    average = ratings.Any() ? Math.Round(ratings.Average(), 2) : 0,
                    distribution,
                    count = ratings.Count
                };

            case SurveyQuestionType.FreeText:
                return new
                {
                    responses = answers
                        .Select(a => JsonSerializer.Deserialize<string>(a.Value))
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Take(100)
                        .ToList()
                };

            default:
                return new { };
        }
    }

    private static List<object?> ParseAnswerValues(string json)
    {
        try
        {
            var val = JsonSerializer.Deserialize<object>(json);
            if (val is JsonElement elem)
            {
                if (elem.ValueKind == JsonValueKind.Array)
                    return elem.EnumerateArray().Select(e => (object?)e.ToString()).ToList();
                return new List<object?> { elem.ToString() };
            }
            return new List<object?> { val };
        }
        catch
        {
            return new List<object?>();
        }
    }

    private static double? ParseNumericValue(string json)
    {
        try
        {
            var val = JsonSerializer.Deserialize<object>(json);
            if (val is JsonElement elem && elem.TryGetDouble(out var d))
                return d;
            if (double.TryParse(val?.ToString(), out var parsed))
                return parsed;
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string FormatAnswerForCsv(string json)
    {
        try
        {
            var val = JsonSerializer.Deserialize<object>(json);
            if (val is JsonElement elem)
            {
                if (elem.ValueKind == JsonValueKind.Array)
                    return string.Join("; ", elem.EnumerateArray().Select(e => e.ToString()));
                return elem.ToString();
            }
            return val?.ToString() ?? "";
        }
        catch
        {
            return json;
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}

// DTOs
public record SurveyListDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public SurveyType Type { get; init; }
    public bool IsAnonymous { get; init; }
    public SurveyStatus Status { get; init; }
    public DateTimeOffset? StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public int QuestionCount { get; init; }
    public int ResponseCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public record SurveyDetailDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public SurveyType Type { get; init; }
    public string? RecurrenceConfig { get; init; }
    public bool IsAnonymous { get; init; }
    public SurveyStatus Status { get; init; }
    public DateTimeOffset? StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public string? TargetGroupIds { get; init; }
    public Guid? SpaceId { get; init; }
    public string? SpaceName { get; init; }
    public IList<SurveyQuestionDto> Questions { get; init; } = new List<SurveyQuestionDto>();
    public int ResponseCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public record SurveyQuestionDto
{
    public Guid Id { get; init; }
    public SurveyQuestionType Type { get; init; }
    public required string QuestionText { get; init; }
    public string? Description { get; init; }
    public string? Options { get; init; }
    public bool IsRequired { get; init; }
    public int SortOrder { get; init; }
    public string? Settings { get; init; }
}

public record SurveyAnalyticsDto
{
    public Guid SurveyId { get; init; }
    public int TotalResponses { get; init; }
    public int CompleteResponses { get; init; }
    public decimal ResponseRate { get; init; }
    public IList<QuestionAnalyticsDto> QuestionAnalytics { get; init; } = new List<QuestionAnalyticsDto>();
}

public record QuestionAnalyticsDto
{
    public Guid QuestionId { get; init; }
    public required string QuestionText { get; init; }
    public SurveyQuestionType Type { get; init; }
    public int TotalAnswers { get; init; }
    public required object Analytics { get; init; }
}

public record CreateSurveyRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public SurveyType? Type { get; init; }
    public string? RecurrenceConfig { get; init; }
    public bool? IsAnonymous { get; init; }
    public DateTimeOffset? StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public string? TargetGroupIds { get; init; }
    public Guid? SpaceId { get; init; }
}

public record UpdateSurveyRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public SurveyType? Type { get; init; }
    public string? RecurrenceConfig { get; init; }
    public bool? IsAnonymous { get; init; }
    public DateTimeOffset? StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public string? TargetGroupIds { get; init; }
    public Guid? SpaceId { get; init; }
}

public record UpdateQuestionsRequest
{
    public IList<SurveyQuestionInput> Questions { get; init; } = new List<SurveyQuestionInput>();
}

public record SurveyQuestionInput
{
    public Guid? Id { get; init; }
    public SurveyQuestionType Type { get; init; }
    public required string QuestionText { get; init; }
    public string? Description { get; init; }
    public string? Options { get; init; }
    public bool? IsRequired { get; init; }
    public string? Settings { get; init; }
}

public record SubmitResponseRequest
{
    public Dictionary<string, object> Answers { get; init; } = new();
}
