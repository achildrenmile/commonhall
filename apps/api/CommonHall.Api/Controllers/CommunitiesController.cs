using System.Text.RegularExpressions;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Api.Controllers;

[Authorize]
public class CommunitiesController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CommunitiesController(
        IApplicationDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all communities (discover).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool myOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Communities
            .Include(c => c.Memberships)
            .Where(c => !c.IsDeleted && !c.IsArchived)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) ||
                                     (c.Description != null && c.Description.ToLower().Contains(term)));
        }

        if (myOnly)
        {
            query = query.Where(c => c.Memberships.Any(m => m.UserId == _currentUser.UserId));
        }

        var communities = await query
            .OrderByDescending(c => c.MemberCount)
            .ThenBy(c => c.Name)
            .Select(c => new CommunityListDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                CoverImageUrl = c.CoverImageUrl,
                Type = c.Type,
                MemberCount = c.MemberCount,
                IsMember = c.Memberships.Any(m => m.UserId == _currentUser.UserId),
                MyRole = c.Memberships
                    .Where(m => m.UserId == _currentUser.UserId)
                    .Select(m => (CommunityMemberRole?)m.Role)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return Ok(communities);
    }

    /// <summary>
    /// Get a community by slug.
    /// </summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var community = await _context.Communities
            .Include(c => c.Memberships)
            .Include(c => c.Space)
            .Where(c => c.Slug == slug && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (community == null)
            return NotFound();

        var membership = community.Memberships.FirstOrDefault(m => m.UserId == _currentUser.UserId);

        // Check access for closed/assigned communities
        if (community.Type == CommunityType.Closed && membership == null)
        {
            return Ok(new CommunityDetailDto
            {
                Id = community.Id,
                Name = community.Name,
                Slug = community.Slug,
                Description = community.Description,
                CoverImageUrl = community.CoverImageUrl,
                Type = community.Type,
                MemberCount = community.MemberCount,
                IsMember = false,
                CanJoin = false,
                IsRestricted = true
            });
        }

        return Ok(new CommunityDetailDto
        {
            Id = community.Id,
            Name = community.Name,
            Slug = community.Slug,
            Description = community.Description,
            CoverImageUrl = community.CoverImageUrl,
            Type = community.Type,
            PostPermission = community.PostPermission,
            MemberCount = community.MemberCount,
            IsArchived = community.IsArchived,
            SpaceId = community.SpaceId,
            SpaceName = community.Space?.Name,
            IsMember = membership != null,
            MyRole = membership?.Role,
            CanJoin = community.Type == CommunityType.Open,
            CanPost = CanUserPost(community, membership),
            CreatedAt = community.CreatedAt
        });
    }

    /// <summary>
    /// Create a new community.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCommunityRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role < UserRole.Editor)
            return Forbid();

        // Generate slug
        var slug = GenerateSlug(request.Name);
        var existingSlug = await _context.Communities
            .AnyAsync(c => c.Slug == slug, cancellationToken);
        if (existingSlug)
            slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";

        var community = new Community
        {
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            CoverImageUrl = request.CoverImageUrl,
            Type = request.Type ?? CommunityType.Open,
            AssignedGroupIds = request.AssignedGroupIds,
            PostPermission = request.PostPermission ?? CommunityPostPermission.MembersOnly,
            SpaceId = request.SpaceId,
            MemberCount = 1
        };

        _context.Communities.Add(community);
        await _context.SaveChangesAsync(cancellationToken);

        // Add creator as admin
        var membership = new CommunityMembership
        {
            CommunityId = community.Id,
            UserId = _currentUser.UserId!.Value,
            Role = CommunityMemberRole.Admin,
            JoinedAt = DateTimeOffset.UtcNow
        };
        _context.CommunityMemberships.Add(membership);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetBySlug), new { slug = community.Slug }, new { id = community.Id, slug = community.Slug });
    }

    /// <summary>
    /// Update a community.
    /// </summary>
    [HttpPut("{slug}")]
    public async Task<IActionResult> Update(
        string slug,
        [FromBody] UpdateCommunityRequest request,
        CancellationToken cancellationToken)
    {
        var community = await _context.Communities
            .Include(c => c.Memberships)
            .FirstOrDefaultAsync(c => c.Slug == slug && !c.IsDeleted, cancellationToken);

        if (community == null)
            return NotFound();

        var membership = community.Memberships.FirstOrDefault(m => m.UserId == _currentUser.UserId);
        if (membership?.Role != CommunityMemberRole.Admin && _currentUser.Role < UserRole.Admin)
            return Forbid();

        if (request.Name != null) community.Name = request.Name;
        if (request.Description != null) community.Description = request.Description;
        if (request.CoverImageUrl != null) community.CoverImageUrl = request.CoverImageUrl;
        if (request.Type.HasValue) community.Type = request.Type.Value;
        if (request.AssignedGroupIds != null) community.AssignedGroupIds = request.AssignedGroupIds;
        if (request.PostPermission.HasValue) community.PostPermission = request.PostPermission.Value;
        if (request.IsArchived.HasValue) community.IsArchived = request.IsArchived.Value;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Community updated" });
    }

    /// <summary>
    /// Join a community.
    /// </summary>
    [HttpPost("{slug}/join")]
    public async Task<IActionResult> Join(string slug, CancellationToken cancellationToken)
    {
        var community = await _context.Communities
            .Include(c => c.Memberships)
            .FirstOrDefaultAsync(c => c.Slug == slug && !c.IsDeleted, cancellationToken);

        if (community == null)
            return NotFound();

        if (community.Type != CommunityType.Open)
            return BadRequest(new { error = "This community is not open for joining" });

        var existingMembership = community.Memberships.FirstOrDefault(m => m.UserId == _currentUser.UserId);
        if (existingMembership != null)
            return BadRequest(new { error = "You are already a member" });

        var membership = new CommunityMembership
        {
            CommunityId = community.Id,
            UserId = _currentUser.UserId!.Value,
            Role = CommunityMemberRole.Member,
            JoinedAt = DateTimeOffset.UtcNow
        };

        _context.CommunityMemberships.Add(membership);
        community.MemberCount++;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Joined community" });
    }

    /// <summary>
    /// Leave a community.
    /// </summary>
    [HttpPost("{slug}/leave")]
    public async Task<IActionResult> Leave(string slug, CancellationToken cancellationToken)
    {
        var community = await _context.Communities
            .Include(c => c.Memberships)
            .FirstOrDefaultAsync(c => c.Slug == slug && !c.IsDeleted, cancellationToken);

        if (community == null)
            return NotFound();

        var membership = community.Memberships.FirstOrDefault(m => m.UserId == _currentUser.UserId);
        if (membership == null)
            return BadRequest(new { error = "You are not a member" });

        // Check if last admin
        if (membership.Role == CommunityMemberRole.Admin)
        {
            var adminCount = community.Memberships.Count(m => m.Role == CommunityMemberRole.Admin);
            if (adminCount <= 1)
                return BadRequest(new { error = "Cannot leave - you are the only admin" });
        }

        _context.CommunityMemberships.Remove(membership);
        community.MemberCount = Math.Max(0, community.MemberCount - 1);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Left community" });
    }

    /// <summary>
    /// Get community members.
    /// </summary>
    [HttpGet("{slug}/members")]
    public async Task<IActionResult> GetMembers(
        string slug,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var community = await _context.Communities
            .FirstOrDefaultAsync(c => c.Slug == slug && !c.IsDeleted, cancellationToken);

        if (community == null)
            return NotFound();

        var query = _context.CommunityMemberships
            .Include(m => m.User)
            .Where(m => m.CommunityId == community.Id)
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.User.FirstName)
            .AsQueryable();

        if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId))
        {
            var cursorMember = await _context.CommunityMemberships
                .FirstOrDefaultAsync(m => m.Id == cursorId, cancellationToken);
            if (cursorMember != null)
            {
                query = query.Where(m => m.JoinedAt < cursorMember.JoinedAt);
            }
        }

        var members = await query
            .Take(limit + 1)
            .Select(m => new CommunityMemberDto
            {
                Id = m.Id,
                UserId = m.UserId,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                Email = m.User.Email,
                ProfilePhotoUrl = m.User.ProfilePhotoUrl,
                Role = m.Role,
                JoinedAt = m.JoinedAt
            })
            .ToListAsync(cancellationToken);

        var hasMore = members.Count > limit;
        if (hasMore)
            members = members.Take(limit).ToList();

        return Ok(new
        {
            items = members,
            nextCursor = hasMore ? members.Last().Id.ToString() : null,
            hasMore
        });
    }

    /// <summary>
    /// Get community posts (social wall).
    /// </summary>
    [HttpGet("{slug}/posts")]
    public async Task<IActionResult> GetPosts(
        string slug,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var community = await _context.Communities
            .Include(c => c.Memberships)
            .FirstOrDefaultAsync(c => c.Slug == slug && !c.IsDeleted, cancellationToken);

        if (community == null)
            return NotFound();

        var isMember = community.Memberships.Any(m => m.UserId == _currentUser.UserId);
        if (community.Type == CommunityType.Closed && !isMember)
            return Forbid();

        var query = _context.CommunityPosts
            .Include(p => p.Author)
            .Include(p => p.Comments.Where(c => !c.IsDeleted))
            .Include(p => p.Reactions)
            .Where(p => p.CommunityId == community.Id && !p.IsDeleted)
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId))
        {
            var cursorPost = await _context.CommunityPosts
                .FirstOrDefaultAsync(p => p.Id == cursorId, cancellationToken);
            if (cursorPost != null)
            {
                query = query.Where(p => !p.IsPinned && p.CreatedAt < cursorPost.CreatedAt);
            }
        }

        var posts = await query
            .Take(limit + 1)
            .Select(p => new CommunityPostDto
            {
                Id = p.Id,
                AuthorId = p.AuthorId,
                AuthorFirstName = p.Author.FirstName,
                AuthorLastName = p.Author.LastName,
                AuthorProfilePhotoUrl = p.Author.ProfilePhotoUrl,
                Body = p.Body,
                ImageUrl = p.ImageUrl,
                IsPinned = p.IsPinned,
                LikeCount = p.LikeCount,
                CommentCount = p.Comments.Count,
                HasLiked = p.Reactions.Any(r => r.UserId == _currentUser.UserId && r.Type == "like"),
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var hasMore = posts.Count > limit;
        if (hasMore)
            posts = posts.Take(limit).ToList();

        return Ok(new
        {
            items = posts,
            nextCursor = hasMore ? posts.Last().Id.ToString() : null,
            hasMore
        });
    }

    /// <summary>
    /// Create a post.
    /// </summary>
    [HttpPost("{slug}/posts")]
    public async Task<IActionResult> CreatePost(
        string slug,
        [FromBody] CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        var community = await _context.Communities
            .Include(c => c.Memberships)
            .FirstOrDefaultAsync(c => c.Slug == slug && !c.IsDeleted, cancellationToken);

        if (community == null)
            return NotFound();

        var membership = community.Memberships.FirstOrDefault(m => m.UserId == _currentUser.UserId);
        if (!CanUserPost(community, membership))
            return Forbid();

        var post = new CommunityPost
        {
            CommunityId = community.Id,
            AuthorId = _currentUser.UserId!.Value,
            Body = request.Body,
            ImageUrl = request.ImageUrl
        };

        _context.CommunityPosts.Add(post);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { id = post.Id });
    }

    /// <summary>
    /// Like/unlike a post.
    /// </summary>
    [HttpPost("{slug}/posts/{postId:guid}/like")]
    public async Task<IActionResult> LikePost(
        string slug,
        Guid postId,
        CancellationToken cancellationToken)
    {
        var post = await _context.CommunityPosts
            .Include(p => p.Reactions)
            .Include(p => p.Community)
            .FirstOrDefaultAsync(p => p.Id == postId && p.Community.Slug == slug && !p.IsDeleted, cancellationToken);

        if (post == null)
            return NotFound();

        var existingReaction = post.Reactions.FirstOrDefault(r => r.UserId == _currentUser.UserId && r.Type == "like");

        if (existingReaction != null)
        {
            _context.CommunityPostReactions.Remove(existingReaction);
            post.LikeCount = Math.Max(0, post.LikeCount - 1);
        }
        else
        {
            var reaction = new CommunityPostReaction
            {
                PostId = postId,
                UserId = _currentUser.UserId!.Value,
                Type = "like"
            };
            _context.CommunityPostReactions.Add(reaction);
            post.LikeCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { liked = existingReaction == null, likeCount = post.LikeCount });
    }

    /// <summary>
    /// Get post comments.
    /// </summary>
    [HttpGet("{slug}/posts/{postId:guid}/comments")]
    public async Task<IActionResult> GetComments(
        string slug,
        Guid postId,
        CancellationToken cancellationToken)
    {
        var post = await _context.CommunityPosts
            .Include(p => p.Community)
            .FirstOrDefaultAsync(p => p.Id == postId && p.Community.Slug == slug && !p.IsDeleted, cancellationToken);

        if (post == null)
            return NotFound();

        var comments = await _context.CommunityPostComments
            .Include(c => c.Author)
            .Where(c => c.PostId == postId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommunityCommentDto
            {
                Id = c.Id,
                AuthorId = c.AuthorId,
                AuthorFirstName = c.Author.FirstName,
                AuthorLastName = c.Author.LastName,
                AuthorProfilePhotoUrl = c.Author.ProfilePhotoUrl,
                Body = c.Body,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(comments);
    }

    /// <summary>
    /// Add a comment.
    /// </summary>
    [HttpPost("{slug}/posts/{postId:guid}/comments")]
    public async Task<IActionResult> AddComment(
        string slug,
        Guid postId,
        [FromBody] AddPostCommentRequest request,
        CancellationToken cancellationToken)
    {
        var post = await _context.CommunityPosts
            .Include(p => p.Community)
            .ThenInclude(c => c.Memberships)
            .FirstOrDefaultAsync(p => p.Id == postId && p.Community.Slug == slug && !p.IsDeleted, cancellationToken);

        if (post == null)
            return NotFound();

        var comment = new CommunityPostComment
        {
            PostId = postId,
            AuthorId = _currentUser.UserId!.Value,
            Body = request.Body
        };

        _context.CommunityPostComments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { id = comment.Id });
    }

    /// <summary>
    /// Delete a community.
    /// </summary>
    [HttpDelete("{slug}")]
    public async Task<IActionResult> Delete(string slug, CancellationToken cancellationToken)
    {
        var community = await _context.Communities
            .Include(c => c.Memberships)
            .FirstOrDefaultAsync(c => c.Slug == slug && !c.IsDeleted, cancellationToken);

        if (community == null)
            return NotFound();

        var membership = community.Memberships.FirstOrDefault(m => m.UserId == _currentUser.UserId);
        if (membership?.Role != CommunityMemberRole.Admin && _currentUser.Role < UserRole.Admin)
            return Forbid();

        community.IsDeleted = true;
        community.DeletedAt = DateTimeOffset.UtcNow;
        community.DeletedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private bool CanUserPost(Community community, CommunityMembership? membership)
    {
        if (community.IsArchived) return false;

        return community.PostPermission switch
        {
            CommunityPostPermission.Anyone => true,
            CommunityPostPermission.MembersOnly => membership != null,
            CommunityPostPermission.AdminsOnly => membership?.Role >= CommunityMemberRole.Moderator,
            _ => false
        };
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }
}

// DTOs
public record CommunityListDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public string? CoverImageUrl { get; init; }
    public CommunityType Type { get; init; }
    public int MemberCount { get; init; }
    public bool IsMember { get; init; }
    public CommunityMemberRole? MyRole { get; init; }
}

public record CommunityDetailDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public string? CoverImageUrl { get; init; }
    public CommunityType Type { get; init; }
    public CommunityPostPermission PostPermission { get; init; }
    public int MemberCount { get; init; }
    public bool IsArchived { get; init; }
    public Guid? SpaceId { get; init; }
    public string? SpaceName { get; init; }
    public bool IsMember { get; init; }
    public CommunityMemberRole? MyRole { get; init; }
    public bool CanJoin { get; init; }
    public bool CanPost { get; init; }
    public bool IsRestricted { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public record CommunityMemberDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public CommunityMemberRole Role { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
}

public record CommunityPostDto
{
    public Guid Id { get; init; }
    public Guid AuthorId { get; init; }
    public string? AuthorFirstName { get; init; }
    public string? AuthorLastName { get; init; }
    public string? AuthorProfilePhotoUrl { get; init; }
    public required string Body { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsPinned { get; init; }
    public int LikeCount { get; init; }
    public int CommentCount { get; init; }
    public bool HasLiked { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public record CommunityCommentDto
{
    public Guid Id { get; init; }
    public Guid AuthorId { get; init; }
    public string? AuthorFirstName { get; init; }
    public string? AuthorLastName { get; init; }
    public string? AuthorProfilePhotoUrl { get; init; }
    public required string Body { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public record CreateCommunityRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? CoverImageUrl { get; init; }
    public CommunityType? Type { get; init; }
    public string? AssignedGroupIds { get; init; }
    public CommunityPostPermission? PostPermission { get; init; }
    public Guid? SpaceId { get; init; }
}

public record UpdateCommunityRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? CoverImageUrl { get; init; }
    public CommunityType? Type { get; init; }
    public string? AssignedGroupIds { get; init; }
    public CommunityPostPermission? PostPermission { get; init; }
    public bool? IsArchived { get; init; }
}

public record CreatePostRequest
{
    public required string Body { get; init; }
    public string? ImageUrl { get; init; }
}

public record AddPostCommentRequest
{
    public required string Body { get; init; }
}
