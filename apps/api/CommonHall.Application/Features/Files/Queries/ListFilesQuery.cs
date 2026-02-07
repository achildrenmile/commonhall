using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.Files.Queries;

public sealed record ListFilesQuery : IRequest<CursorPaginatedResult<StoredFileDto>>
{
    public Guid? CollectionId { get; init; }
    public string? Search { get; init; }
    public string? MimeTypeFilter { get; init; }
    public string? Cursor { get; init; }
    public int Size { get; init; } = 20;
}
