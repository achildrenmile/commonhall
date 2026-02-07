namespace CommonHall.Application.Interfaces;

public interface IFileValidationService
{
    bool IsAllowedMimeType(string mimeType);
    bool IsDangerousExtension(string fileName);
    Task<string?> DetectMimeTypeAsync(Stream stream, CancellationToken cancellationToken = default);
    bool ValidateFileSizeBytes(long sizeBytes);
}
