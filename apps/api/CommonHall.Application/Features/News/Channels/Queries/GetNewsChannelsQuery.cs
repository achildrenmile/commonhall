using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.News.Channels.Queries;

public sealed record GetNewsChannelsQuery : IRequest<List<NewsChannelDto>>
{
    public Guid? SpaceId { get; init; }
}
