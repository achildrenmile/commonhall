using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class Space : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public Guid? ParentSpaceId { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public Space? ParentSpace { get; set; }
    public ICollection<Space> ChildSpaces { get; set; } = new List<Space>();
    public ICollection<SpaceAdministrator> Administrators { get; set; } = new List<SpaceAdministrator>();
    public ICollection<Page> Pages { get; set; } = new List<Page>();
    public ICollection<NewsChannel> NewsChannels { get; set; } = new List<NewsChannel>();
    public ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();
    public ICollection<FileCollection> FileCollections { get; set; } = new List<FileCollection>();
}
