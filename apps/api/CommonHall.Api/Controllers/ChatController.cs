using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Api.Controllers;

[Authorize]
public class ChatController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ChatController(
        IApplicationDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get user's conversations.
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        var conversations = await _context.ConversationMembers
            .Include(cm => cm.Conversation)
            .ThenInclude(c => c.Members)
            .ThenInclude(m => m.User)
            .Where(cm => cm.UserId == _currentUser.UserId)
            .OrderByDescending(cm => cm.Conversation.LastMessageAt ?? cm.Conversation.CreatedAt)
            .Select(cm => new ConversationDto
            {
                Id = cm.Conversation.Id,
                Type = cm.Conversation.Type,
                Name = cm.Conversation.Name ?? GetConversationName(cm.Conversation, _currentUser.UserId!.Value),
                LastMessageAt = cm.Conversation.LastMessageAt,
                LastMessagePreview = cm.Conversation.LastMessagePreview,
                IsMuted = cm.IsMuted,
                UnreadCount = GetUnreadCount(cm),
                Members = cm.Conversation.Members.Select(m => new ConversationMemberDto
                {
                    UserId = m.UserId,
                    FirstName = m.User.FirstName,
                    LastName = m.User.LastName,
                    ProfilePhotoUrl = m.User.ProfilePhotoUrl
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(conversations);
    }

    /// <summary>
    /// Create a conversation.
    /// </summary>
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        // For direct messages, check if conversation already exists
        if (request.Type == ConversationType.Direct && request.MemberIds.Count == 1)
        {
            var otherUserId = request.MemberIds[0];
            var existingConversation = await _context.Conversations
                .Include(c => c.Members)
                .Where(c => c.Type == ConversationType.Direct)
                .Where(c => c.Members.Any(m => m.UserId == _currentUser.UserId) &&
                           c.Members.Any(m => m.UserId == otherUserId))
                .FirstOrDefaultAsync(cancellationToken);

            if (existingConversation != null)
            {
                return Ok(new { id = existingConversation.Id, existing = true });
            }
        }

        var conversation = new Conversation
        {
            Type = request.Type ?? ConversationType.Group,
            Name = request.Name,
            CreatedById = _currentUser.UserId!.Value
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);

        // Add creator
        _context.ConversationMembers.Add(new ConversationMember
        {
            ConversationId = conversation.Id,
            UserId = _currentUser.UserId!.Value,
            JoinedAt = DateTimeOffset.UtcNow
        });

        // Add other members
        foreach (var memberId in request.MemberIds)
        {
            if (memberId != _currentUser.UserId)
            {
                _context.ConversationMembers.Add(new ConversationMember
                {
                    ConversationId = conversation.Id,
                    UserId = memberId,
                    JoinedAt = DateTimeOffset.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { id = conversation.Id });
    }

    /// <summary>
    /// Get messages for a conversation.
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        // Verify membership
        var membership = await _context.ConversationMembers
            .FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == _currentUser.UserId, cancellationToken);

        if (membership == null)
            return Forbid();

        var query = _context.Messages
            .Include(m => m.Author)
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId))
        {
            var cursorMessage = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == cursorId, cancellationToken);
            if (cursorMessage != null)
            {
                query = query.Where(m => m.CreatedAt < cursorMessage.CreatedAt);
            }
        }

        var messages = await query
            .Take(limit + 1)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                AuthorId = m.AuthorId,
                AuthorFirstName = m.Author.FirstName,
                AuthorLastName = m.Author.LastName,
                AuthorProfilePhotoUrl = m.Author.ProfilePhotoUrl,
                Body = m.IsDeleted ? "[Message deleted]" : m.Body,
                Attachments = m.Attachments,
                IsDeleted = m.IsDeleted,
                EditedAt = m.EditedAt,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > limit;
        if (hasMore)
            messages = messages.Take(limit).ToList();

        // Reverse to get chronological order
        messages.Reverse();

        return Ok(new
        {
            items = messages,
            nextCursor = hasMore ? messages.First().Id.ToString() : null,
            hasMore
        });
    }

    /// <summary>
    /// Send a message.
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        // Verify membership
        var membership = await _context.ConversationMembers
            .FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == _currentUser.UserId, cancellationToken);

        if (membership == null)
            return Forbid();

        var message = new Message
        {
            ConversationId = conversationId,
            AuthorId = _currentUser.UserId!.Value,
            Body = request.Body,
            Attachments = request.Attachments
        };

        _context.Messages.Add(message);

        // Update conversation
        var conversation = await _context.Conversations.FindAsync(new object[] { conversationId }, cancellationToken);
        if (conversation != null)
        {
            conversation.LastMessageAt = DateTimeOffset.UtcNow;
            conversation.LastMessagePreview = request.Body.Length > 100
                ? request.Body[..100] + "..."
                : request.Body;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new MessageDto
        {
            Id = message.Id,
            AuthorId = message.AuthorId,
            Body = message.Body,
            Attachments = message.Attachments,
            CreatedAt = message.CreatedAt
        });
    }

    /// <summary>
    /// Edit a message.
    /// </summary>
    [HttpPut("messages/{messageId:guid}")]
    public async Task<IActionResult> EditMessage(
        Guid messageId,
        [FromBody] EditMessageRequest request,
        CancellationToken cancellationToken)
    {
        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message == null)
            return NotFound();

        if (message.AuthorId != _currentUser.UserId)
            return Forbid();

        if (message.IsDeleted)
            return BadRequest(new { error = "Cannot edit a deleted message" });

        message.Body = request.Body;
        message.EditedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Message updated" });
    }

    /// <summary>
    /// Delete a message.
    /// </summary>
    [HttpDelete("messages/{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message == null)
            return NotFound();

        if (message.AuthorId != _currentUser.UserId)
            return Forbid();

        message.IsDeleted = true;
        message.Body = "";

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Mark conversation as read.
    /// </summary>
    [HttpPut("conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var membership = await _context.ConversationMembers
            .FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == _currentUser.UserId, cancellationToken);

        if (membership == null)
            return NotFound();

        membership.LastReadAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Marked as read" });
    }

    /// <summary>
    /// Get total unread count across all conversations.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var memberships = await _context.ConversationMembers
            .Include(m => m.Conversation)
            .ThenInclude(c => c.Messages)
            .Where(m => m.UserId == _currentUser.UserId && !m.IsMuted)
            .ToListAsync(cancellationToken);

        var totalUnread = 0;
        foreach (var membership in memberships)
        {
            var unreadCount = membership.Conversation.Messages
                .Count(m => !m.IsDeleted &&
                           m.AuthorId != _currentUser.UserId &&
                           (membership.LastReadAt == null || m.CreatedAt > membership.LastReadAt));
            totalUnread += unreadCount;
        }

        return Ok(new { unreadCount = totalUnread });
    }

    /// <summary>
    /// Toggle mute for a conversation.
    /// </summary>
    [HttpPut("conversations/{conversationId:guid}/mute")]
    public async Task<IActionResult> ToggleMute(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var membership = await _context.ConversationMembers
            .FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == _currentUser.UserId, cancellationToken);

        if (membership == null)
            return NotFound();

        membership.IsMuted = !membership.IsMuted;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { isMuted = membership.IsMuted });
    }

    private static string GetConversationName(Conversation conversation, Guid currentUserId)
    {
        if (!string.IsNullOrEmpty(conversation.Name))
            return conversation.Name;

        var otherMembers = conversation.Members
            .Where(m => m.UserId != currentUserId)
            .Select(m => $"{m.User.FirstName} {m.User.LastName}".Trim())
            .Take(3)
            .ToList();

        if (otherMembers.Count == 0)
            return "Empty conversation";

        return string.Join(", ", otherMembers);
    }

    private static int GetUnreadCount(ConversationMember membership)
    {
        if (membership.Conversation.Messages == null)
            return 0;

        return membership.Conversation.Messages
            .Count(m => !m.IsDeleted &&
                       m.AuthorId != membership.UserId &&
                       (membership.LastReadAt == null || m.CreatedAt > membership.LastReadAt));
    }
}

// DTOs
public record ConversationDto
{
    public Guid Id { get; init; }
    public ConversationType Type { get; init; }
    public required string Name { get; init; }
    public DateTimeOffset? LastMessageAt { get; init; }
    public string? LastMessagePreview { get; init; }
    public bool IsMuted { get; init; }
    public int UnreadCount { get; init; }
    public IList<ConversationMemberDto> Members { get; init; } = new List<ConversationMemberDto>();
}

public record ConversationMemberDto
{
    public Guid UserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? ProfilePhotoUrl { get; init; }
}

public record MessageDto
{
    public Guid Id { get; init; }
    public Guid AuthorId { get; init; }
    public string? AuthorFirstName { get; init; }
    public string? AuthorLastName { get; init; }
    public string? AuthorProfilePhotoUrl { get; init; }
    public required string Body { get; init; }
    public string? Attachments { get; init; }
    public bool IsDeleted { get; init; }
    public DateTimeOffset? EditedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public record CreateConversationRequest
{
    public ConversationType? Type { get; init; }
    public string? Name { get; init; }
    public IList<Guid> MemberIds { get; init; } = new List<Guid>();
}

public record SendMessageRequest
{
    public required string Body { get; init; }
    public string? Attachments { get; init; }
}

public record EditMessageRequest
{
    public required string Body { get; init; }
}
