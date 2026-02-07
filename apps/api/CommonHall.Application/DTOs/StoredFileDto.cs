using CommonHall.Domain.Entities;

namespace CommonHall.Application.DTOs;

public sealed record StoredFileDto
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
    public required string OriginalName { get; init; }
    public required string MimeType { get; init; }
    public required long SizeBytes { get; init; }
    public required string Url { get; init; }
    public string? ThumbnailUrl { get; init; }
    public Guid? CollectionId { get; init; }
    public string? CollectionName { get; init; }
    public required UserSummaryDto UploadedBy { get; init; }
    public string? AltText { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public static StoredFileDto FromEntity(StoredFile file, User uploader, string baseUrl)
    {
        return new StoredFileDto
        {
            Id = file.Id,
            FileName = file.FileName,
            OriginalName = file.OriginalName,
            MimeType = file.MimeType,
            SizeBytes = file.SizeBytes,
            Url = $"{baseUrl}/api/v1/files/{file.Id}/download",
            ThumbnailUrl = file.ThumbnailPath != null ? $"{baseUrl}/api/v1/files/{file.Id}/thumbnail" : null,
            CollectionId = file.CollectionId,
            CollectionName = file.Collection?.Name,
            UploadedBy = UserSummaryDto.FromEntity(uploader),
            AltText = file.AltText,
            CreatedAt = file.CreatedAt
        };
    }
}
