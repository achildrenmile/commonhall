using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Users.Queries;

public sealed record UserProfileNewsArticleDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public string? TeaserImageUrl { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public required string ChannelName { get; init; }
    public required string ChannelSlug { get; init; }
}

public sealed record UserProfileGroupDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
}

public sealed record UserProfileDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Department { get; init; }
    public string? Location { get; init; }
    public string? JobTitle { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Bio { get; init; }
    public required UserRole Role { get; init; }
    public required bool IsActive { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required IReadOnlyList<UserProfileGroupDto> Groups { get; init; }
    public required IReadOnlyList<UserProfileNewsArticleDto> RecentArticles { get; init; }
}

public sealed record GetUserProfileQuery(Guid Id) : IRequest<UserProfileDto>;

public sealed class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IApplicationDbContext _context;

    public GetUserProfileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.GroupMemberships)
                .ThenInclude(gm => gm.Group)
            .FirstOrDefaultAsync(u => u.Id == request.Id && !u.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"User with ID {request.Id} not found");

        var recentArticles = await _context.NewsArticles
            .Where(a => (a.AuthorId == request.Id || a.DisplayAuthorId == request.Id)
                        && a.Status == NewsArticleStatus.Published
                        && !a.IsDeleted)
            .OrderByDescending(a => a.PublishedAt)
            .Take(5)
            .Select(a => new UserProfileNewsArticleDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                TeaserImageUrl = a.TeaserImageUrl,
                PublishedAt = a.PublishedAt,
                ChannelName = a.Channel!.Name,
                ChannelSlug = a.Channel.Slug
            })
            .ToListAsync(cancellationToken);

        var groups = user.GroupMemberships
            .Where(gm => !gm.Group.IsDeleted)
            .Select(gm => new UserProfileGroupDto
            {
                Id = gm.Group.Id,
                Name = gm.Group.Name,
                Slug = gm.Group.Slug,
                Description = gm.Group.Description
            })
            .OrderBy(g => g.Name)
            .ToList();

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            Department = user.Department,
            Location = user.Location,
            JobTitle = user.JobTitle,
            PhoneNumber = user.PhoneNumber,
            Bio = user.Bio,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            Groups = groups,
            RecentArticles = recentArticles
        };
    }
}
