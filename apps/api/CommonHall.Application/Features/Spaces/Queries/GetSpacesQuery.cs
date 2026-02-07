using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.Spaces.Queries;

public sealed record GetSpacesQuery : IRequest<List<SpaceDto>>
{
    public Guid? ParentSpaceId { get; init; }
    public bool IncludeChildren { get; init; } = false;
}
