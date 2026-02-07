namespace CommonHall.Application.Interfaces;

public interface IFileStorageService
{
    Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string mimeType, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<Stream?> GetStreamAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<string?> GenerateThumbnailAsync(string storagePath, int width = 200, int height = 200, CancellationToken cancellationToken = default);
    string GetFullPath(string storagePath);
}

public sealed record FileUploadResult
{
    public required string StoragePath { get; init; }
    public required long SizeBytes { get; init; }
}
