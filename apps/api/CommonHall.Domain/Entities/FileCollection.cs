using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class FileCollection : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? SpaceId { get; set; }

    // Navigation properties
    public Space? Space { get; set; }
    public ICollection<StoredFile> Files { get; set; } = new List<StoredFile>();
}
