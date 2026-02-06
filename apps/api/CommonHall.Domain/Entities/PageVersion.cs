using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class PageVersion : BaseEntity
{
    public Guid PageId { get; set; }
    public string Content { get; set; } = "[]";
    public int VersionNumber { get; set; }
    public string? ChangeDescription { get; set; }

    // Navigation properties
    public Page Page { get; set; } = null!;
}
