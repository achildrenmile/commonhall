using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.Collections.Queries;

public sealed record ListCollectionsQuery : IRequest<List<FileCollectionDto>>
{
    public Guid? SpaceId { get; init; }
    public string? Search { get; init; }
}
