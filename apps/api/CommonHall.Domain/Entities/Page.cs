using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class Page : BaseEntity, ISoftDeletable
{
    public Guid SpaceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = "[]";
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public string? MetaDescription { get; set; }
    public int SortOrder { get; set; }
    public bool IsHomePage { get; set; }
    public string? VisibilityRule { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public Space Space { get; set; } = null!;
    public ICollection<PageVersion> Versions { get; set; } = new List<PageVersion>();
}
