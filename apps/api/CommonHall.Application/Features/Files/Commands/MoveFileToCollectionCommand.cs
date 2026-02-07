using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.Files.Commands;

public sealed record MoveFileToCollectionCommand : IRequest<StoredFileDto>
{
    public required Guid FileId { get; init; }
    public Guid? CollectionId { get; init; }
}
