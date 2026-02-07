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
public class FormsController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public FormsController(
        IApplicationDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all forms with optional filtering.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var query = _context.Forms
            .Include(f => f.Submissions)
            .Where(f => !f.IsDeleted)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(f => f.IsActive == isActive.Value);

        var forms = await query
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FormListDto
            {
                Id = f.Id,
                Title = f.Title,
                Description = f.Description,
                IsActive = f.IsActive,
                SubmissionCount = f.Submissions.Count,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(forms);
    }

    /// <summary>
    /// Get a single form by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var form = await _context.Forms
            .Include(f => f.Space)
            .Where(f => f.Id == id && !f.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (form == null)
            return NotFound();

        var submissionCount = await _context.FormSubmissions
            .CountAsync(s => s.FormId == id, cancellationToken);

        return Ok(new FormDetailDto
        {
            Id = form.Id,
            Title = form.Title,
            Description = form.Description,
            Fields = form.Fields,
            NotificationEmail = form.NotificationEmail,
            ConfirmationMessage = form.ConfirmationMessage,
            IsActive = form.IsActive,
            SpaceId = form.SpaceId,
            SpaceName = form.Space?.Name,
            SubmissionCount = submissionCount,
            CreatedAt = form.CreatedAt,
            UpdatedAt = form.UpdatedAt
        });
    }

    /// <summary>
    /// Get a form for public submission (minimal info).
    /// </summary>
    [HttpGet("{id:guid}/public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic(Guid id, CancellationToken cancellationToken)
    {
        var form = await _context.Forms
            .Where(f => f.Id == id && !f.IsDeleted && f.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (form == null)
            return NotFound();

        return Ok(new FormPublicDto
        {
            Id = form.Id,
            Title = form.Title,
            Description = form.Description,
            Fields = form.Fields,
            ConfirmationMessage = form.ConfirmationMessage
        });
    }

    /// <summary>
    /// Create a new form.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateFormRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var form = new Form
        {
            Title = request.Title,
            Description = request.Description,
            Fields = request.Fields ?? "[]",
            NotificationEmail = request.NotificationEmail,
            ConfirmationMessage = request.ConfirmationMessage,
            IsActive = false,
            SpaceId = request.SpaceId
        };

        _context.Forms.Add(form);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = form.Id }, new FormDetailDto
        {
            Id = form.Id,
            Title = form.Title,
            Description = form.Description,
            Fields = form.Fields,
            NotificationEmail = form.NotificationEmail,
            ConfirmationMessage = form.ConfirmationMessage,
            IsActive = form.IsActive,
            SpaceId = form.SpaceId,
            SubmissionCount = 0,
            CreatedAt = form.CreatedAt,
            UpdatedAt = form.UpdatedAt
        });
    }

    /// <summary>
    /// Update a form.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateFormRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var form = await _context.Forms
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);

        if (form == null)
            return NotFound();

        if (request.Title != null) form.Title = request.Title;
        if (request.Description != null) form.Description = request.Description;
        if (request.Fields != null) form.Fields = request.Fields;
        if (request.NotificationEmail != null) form.NotificationEmail = request.NotificationEmail;
        if (request.ConfirmationMessage != null) form.ConfirmationMessage = request.ConfirmationMessage;
        if (request.IsActive.HasValue) form.IsActive = request.IsActive.Value;
        if (request.SpaceId.HasValue) form.SpaceId = request.SpaceId == Guid.Empty ? null : request.SpaceId;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Form updated" });
    }

    /// <summary>
    /// Submit a form response.
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [AllowAnonymous]
    public async Task<IActionResult> Submit(
        Guid id,
        [FromBody] SubmitFormRequest request,
        CancellationToken cancellationToken)
    {
        var form = await _context.Forms
            .Where(f => f.Id == id && !f.IsDeleted && f.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (form == null)
            return NotFound();

        // Parse field definitions to validate
        var fields = JsonSerializer.Deserialize<List<FormFieldDefinition>>(form.Fields) ?? new List<FormFieldDefinition>();

        // Validate required fields
        foreach (var field in fields.Where(f => f.Required))
        {
            if (!request.Data.TryGetValue(field.Name, out var value) ||
                value == null ||
                (value is JsonElement elem && elem.ValueKind == JsonValueKind.Null) ||
                (value is string s && string.IsNullOrWhiteSpace(s)))
            {
                return BadRequest(new { error = $"Field '{field.Label ?? field.Name}' is required" });
            }
        }

        var submission = new FormSubmission
        {
            FormId = id,
            UserId = _currentUser.IsAuthenticated ? _currentUser.UserId : null,
            Data = JsonSerializer.Serialize(request.Data),
            Attachments = request.Attachments != null ? JsonSerializer.Serialize(request.Attachments) : null
        };

        _context.FormSubmissions.Add(submission);
        await _context.SaveChangesAsync(cancellationToken);

        // TODO: Send notification email if configured

        return Ok(new
        {
            message = "Form submitted successfully",
            submissionId = submission.Id,
            confirmationMessage = form.ConfirmationMessage
        });
    }

    /// <summary>
    /// Get form submissions.
    /// </summary>
    [HttpGet("{id:guid}/submissions")]
    public async Task<IActionResult> GetSubmissions(
        Guid id,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var form = await _context.Forms
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);

        if (form == null)
            return NotFound();

        var query = _context.FormSubmissions
            .Include(s => s.User)
            .Where(s => s.FormId == id)
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId))
        {
            var cursorSubmission = await _context.FormSubmissions
                .FirstOrDefaultAsync(s => s.Id == cursorId, cancellationToken);

            if (cursorSubmission != null)
            {
                query = query.Where(s => s.CreatedAt < cursorSubmission.CreatedAt);
            }
        }

        var submissions = await query
            .Take(limit + 1)
            .Select(s => new FormSubmissionDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserName = s.User != null ? $"{s.User.FirstName} {s.User.LastName}" : null,
                UserEmail = s.User != null ? s.User.Email : null,
                Data = s.Data,
                Attachments = s.Attachments,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var hasMore = submissions.Count > limit;
        if (hasMore)
            submissions = submissions.Take(limit).ToList();

        return Ok(new
        {
            items = submissions,
            nextCursor = hasMore ? submissions.Last().Id.ToString() : null,
            hasMore
        });
    }

    /// <summary>
    /// Export form submissions as CSV.
    /// </summary>
    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> ExportCsv(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var form = await _context.Forms
            .Include(f => f.Submissions)
            .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);

        if (form == null)
            return NotFound();

        var fields = JsonSerializer.Deserialize<List<FormFieldDefinition>>(form.Fields) ?? new List<FormFieldDefinition>();

        var sb = new StringBuilder();

        // Header
        var headers = new List<string> { "Submission ID", "User Email", "Submitted At" };
        headers.AddRange(fields.Select(f => f.Label ?? f.Name));
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        // Data rows
        foreach (var submission in form.Submissions.OrderByDescending(s => s.CreatedAt))
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(submission.Data)
                ?? new Dictionary<string, JsonElement>();

            var row = new List<string>
            {
                submission.Id.ToString(),
                submission.User?.Email ?? "",
                submission.CreatedAt.ToString("O")
            };

            foreach (var field in fields)
            {
                var value = data.TryGetValue(field.Name, out var val)
                    ? FormatJsonValueForCsv(val)
                    : "";
                row.Add(value);
            }

            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"form-{id}-submissions.csv");
    }

    /// <summary>
    /// Delete a form.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var form = await _context.Forms
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);

        if (form == null)
            return NotFound();

        form.IsDeleted = true;
        form.DeletedAt = DateTimeOffset.UtcNow;
        form.DeletedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static string FormatJsonValueForCsv(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Array => string.Join("; ", element.EnumerateArray().Select(e => e.ToString())),
            JsonValueKind.Null => "",
            JsonValueKind.Undefined => "",
            _ => element.ToString()
        };
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}

// DTOs
public record FormListDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int SubmissionCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public record FormDetailDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Fields { get; init; }
    public string? NotificationEmail { get; init; }
    public string? ConfirmationMessage { get; init; }
    public bool IsActive { get; init; }
    public Guid? SpaceId { get; init; }
    public string? SpaceName { get; init; }
    public int SubmissionCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public record FormPublicDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Fields { get; init; }
    public string? ConfirmationMessage { get; init; }
}

public record FormSubmissionDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserEmail { get; init; }
    public required string Data { get; init; }
    public string? Attachments { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public record CreateFormRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Fields { get; init; }
    public string? NotificationEmail { get; init; }
    public string? ConfirmationMessage { get; init; }
    public Guid? SpaceId { get; init; }
}

public record UpdateFormRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Fields { get; init; }
    public string? NotificationEmail { get; init; }
    public string? ConfirmationMessage { get; init; }
    public bool? IsActive { get; init; }
    public Guid? SpaceId { get; init; }
}

public record SubmitFormRequest
{
    public Dictionary<string, object> Data { get; init; } = new();
    public List<string>? Attachments { get; init; }
}

public record FormFieldDefinition
{
    public required string Name { get; init; }
    public string? Label { get; init; }
    public string Type { get; init; } = "text";
    public bool Required { get; init; }
    public string? Placeholder { get; init; }
    public List<string>? Options { get; init; }
}
