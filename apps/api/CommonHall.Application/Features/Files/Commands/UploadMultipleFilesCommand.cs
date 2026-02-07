using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.Files.Commands;

public sealed record UploadMultipleFilesCommand : IRequest<List<StoredFileDto>>
{
    public required List<FileUploadItem> Files { get; init; }
    public Guid? CollectionId { get; init; }
}

public sealed record FileUploadItem
{
    public required Stream Stream { get; init; }
    public required string OriginalName { get; init; }
    public required string MimeType { get; init; }
}
