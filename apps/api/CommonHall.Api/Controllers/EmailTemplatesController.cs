using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Api.Controllers;

[Authorize]
public class EmailTemplatesController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public EmailTemplatesController(
        IApplicationDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all email templates.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] EmailTemplateCategory? category,
        [FromQuery] bool? systemOnly,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var query = _context.EmailTemplates
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (category.HasValue)
            query = query.Where(t => t.Category == category.Value);

        if (systemOnly == true)
            query = query.Where(t => t.IsSystem);

        var templates = await query
            .OrderBy(t => t.Name)
            .Select(t => new EmailTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                ThumbnailUrl = t.ThumbnailUrl,
                IsSystem = t.IsSystem,
                Category = t.Category,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(templates);
    }

    /// <summary>
    /// Get a single email template by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        var template = await _context.EmailTemplates
            .Where(t => t.Id == id && !t.IsDeleted)
            .Select(t => new EmailTemplateDetailDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Content = t.Content,
                ThumbnailUrl = t.ThumbnailUrl,
                IsSystem = t.IsSystem,
                Category = t.Category,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (template == null)
            return NotFound();

        return Ok(template);
    }

    /// <summary>
    /// Create a new email template.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmailTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var template = new EmailTemplate
        {
            Name = request.Name,
            Description = request.Description,
            Content = request.Content ?? "[]",
            ThumbnailUrl = request.ThumbnailUrl,
            Category = request.Category ?? EmailTemplateCategory.Newsletter,
            IsSystem = false
        };

        _context.EmailTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = template.Id }, new EmailTemplateDetailDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Content = template.Content,
            ThumbnailUrl = template.ThumbnailUrl,
            IsSystem = template.IsSystem,
            Category = template.Category,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        });
    }

    /// <summary>
    /// Update an email template.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEmailTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);

        if (template == null)
            return NotFound();

        if (template.IsSystem)
            return BadRequest(new { error = "System templates cannot be modified" });

        if (request.Name != null)
            template.Name = request.Name;
        if (request.Description != null)
            template.Description = request.Description;
        if (request.Content != null)
            template.Content = request.Content;
        if (request.ThumbnailUrl != null)
            template.ThumbnailUrl = request.ThumbnailUrl;
        if (request.Category.HasValue)
            template.Category = request.Category.Value;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new EmailTemplateDetailDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Content = template.Content,
            ThumbnailUrl = template.ThumbnailUrl,
            IsSystem = template.IsSystem,
            Category = template.Category,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        });
    }

    /// <summary>
    /// Delete an email template.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Admin)
            return Forbid();

        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);

        if (template == null)
            return NotFound();

        if (template.IsSystem)
            return BadRequest(new { error = "System templates cannot be deleted" });

        template.IsDeleted = true;
        template.DeletedAt = DateTimeOffset.UtcNow;
        template.DeletedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

public record CreateEmailTemplateRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Content { get; init; }
    public string? ThumbnailUrl { get; init; }
    public EmailTemplateCategory? Category { get; init; }
}

public record UpdateEmailTemplateRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Content { get; init; }
    public string? ThumbnailUrl { get; init; }
    public EmailTemplateCategory? Category { get; init; }
}

public record EmailTemplateDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public bool IsSystem { get; init; }
    public EmailTemplateCategory Category { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public record EmailTemplateDetailDto : EmailTemplateDto
{
    public required string Content { get; init; }
}
