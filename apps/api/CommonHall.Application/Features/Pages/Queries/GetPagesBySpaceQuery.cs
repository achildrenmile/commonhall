using CommonHall.Application.DTOs;
using CommonHall.Domain.Enums;
using MediatR;

namespace CommonHall.Application.Features.Pages.Queries;

public sealed record GetPagesBySpaceQuery : IRequest<List<PageListDto>>
{
    public Guid SpaceId { get; init; }
    public ContentStatus? Status { get; init; }
}
