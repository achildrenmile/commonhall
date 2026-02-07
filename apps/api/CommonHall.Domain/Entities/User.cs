using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace CommonHall.Domain.Entities;

public sealed class User : IdentityUser<Guid>, ISoftDeletable
{
    public string DisplayName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? Department { get; set; }
    public string? Location { get; set; }
    public string? JobTitle { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? HireDate { get; set; }
    public DateTimeOffset? Birthday { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? ExternalId { get; set; }
    public UserRole Role { get; set; } = UserRole.Employee;

    // BaseEntity-like properties (can't inherit due to IdentityUser)
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public ICollection<UserGroupMembership> GroupMemberships { get; set; } = new List<UserGroupMembership>();
    public ICollection<SpaceAdministrator> AdministeredSpaces { get; set; } = new List<SpaceAdministrator>();
    public ICollection<NewsArticle> AuthoredArticles { get; set; } = new List<NewsArticle>();
    public ICollection<NewsArticle> GhostAuthoredArticles { get; set; } = new List<NewsArticle>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
    public ICollection<StoredFile> UploadedFiles { get; set; } = new List<StoredFile>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
