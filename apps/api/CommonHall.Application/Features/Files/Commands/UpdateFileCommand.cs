using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.Files.Commands;

public sealed record UpdateFileCommand : IRequest<StoredFileDto>
{
    public required Guid Id { get; init; }
    public string? AltText { get; init; }
}
