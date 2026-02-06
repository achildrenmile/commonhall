using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class StoredFile : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public Guid? CollectionId { get; set; }
    public Guid UploadedBy { get; set; }
    public string? AltText { get; set; }

    // Navigation properties
    public FileCollection? Collection { get; set; }
    public User Uploader { get; set; } = null!;
}
