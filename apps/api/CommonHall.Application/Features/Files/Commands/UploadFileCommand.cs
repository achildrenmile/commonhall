using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.Files.Commands;

public sealed record UploadFileCommand : IRequest<StoredFileDto>
{
    public required Stream Stream { get; init; }
    public required string OriginalName { get; init; }
    public required string MimeType { get; init; }
    public Guid? CollectionId { get; init; }
}
