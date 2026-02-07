using System.Security.Claims;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IApplicationDbContext _context;

    public ChatHub(IApplicationDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            await base.OnConnectedAsync();
            return;
        }

        // Join all conversation groups the user is a member of
        var conversationIds = await _context.ConversationMembers
            .Where(m => m.UserId == userId.Value)
            .Select(m => m.ConversationId)
            .ToListAsync();

        foreach (var conversationId in conversationIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != null)
        {
            // Leave all groups (SignalR handles this automatically, but being explicit)
            var conversationIds = await _context.ConversationMembers
                .Where(m => m.UserId == userId.Value)
                .Select(m => m.ConversationId)
                .ToListAsync();

            foreach (var conversationId in conversationIds)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Send a message to a conversation.
    /// </summary>
    public async Task SendMessage(Guid conversationId, string body, string? attachments = null)
    {
        var userId = GetUserId();
        if (userId == null) return;

        // Verify membership
        var membership = await _context.ConversationMembers
            .FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == userId.Value);

        if (membership == null) return;

        // Get user info
        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null) return;

        // Create message
        var message = new Message
        {
            ConversationId = conversationId,
            AuthorId = userId.Value,
            Body = body,
            Attachments = attachments
        };

        _context.Messages.Add(message);

        // Update conversation
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.LastMessageAt = DateTimeOffset.UtcNow;
            conversation.LastMessagePreview = body.Length > 100 ? body[..100] + "..." : body;
        }

        await _context.SaveChangesAsync();

        // Broadcast to all members in the conversation
        var messageDto = new
        {
            id = message.Id,
            conversationId,
            authorId = userId.Value,
            authorFirstName = user.FirstName,
            authorLastName = user.LastName,
            authorProfilePhotoUrl = user.ProfilePhotoUrl,
            body,
            attachments,
            createdAt = message.CreatedAt
        };

        await Clients.Group($"conversation:{conversationId}").SendAsync("ReceiveMessage", messageDto);

        // Update unread counts for other members
        var otherMemberIds = await _context.ConversationMembers
            .Where(m => m.ConversationId == conversationId && m.UserId != userId.Value)
            .Select(m => m.UserId.ToString())
            .ToListAsync();

        foreach (var memberId in otherMemberIds)
        {
            await Clients.User(memberId).SendAsync("UnreadCountUpdated", new { conversationId });
        }
    }

    /// <summary>
    /// Signal that user started typing.
    /// </summary>
    public async Task TypingStarted(Guid conversationId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null) return;

        await Clients.OthersInGroup($"conversation:{conversationId}").SendAsync("UserTyping", new
        {
            conversationId,
            userId = userId.Value,
            firstName = user.FirstName,
            lastName = user.LastName,
            isTyping = true
        });
    }

    /// <summary>
    /// Signal that user stopped typing.
    /// </summary>
    public async Task TypingStopped(Guid conversationId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        await Clients.OthersInGroup($"conversation:{conversationId}").SendAsync("UserTyping", new
        {
            conversationId,
            userId = userId.Value,
            isTyping = false
        });
    }

    /// <summary>
    /// Mark a conversation as read.
    /// </summary>
    public async Task MarkAsRead(Guid conversationId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var membership = await _context.ConversationMembers
            .FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == userId.Value);

        if (membership == null) return;

        membership.LastReadAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        // Notify other members that this user read the messages
        await Clients.OthersInGroup($"conversation:{conversationId}").SendAsync("MessageRead", new
        {
            conversationId,
            userId = userId.Value,
            readAt = membership.LastReadAt
        });
    }

    /// <summary>
    /// Join a new conversation (called after creating a new conversation).
    /// </summary>
    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        // Verify membership
        var isMember = await _context.ConversationMembers
            .AnyAsync(m => m.ConversationId == conversationId && m.UserId == userId.Value);

        if (isMember)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");
        }
    }

    private Guid? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
