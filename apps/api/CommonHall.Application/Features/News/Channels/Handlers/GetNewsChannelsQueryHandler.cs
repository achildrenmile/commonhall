using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Channels.Queries;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Channels.Handlers;

public sealed class GetNewsChannelsQueryHandler : IRequestHandler<GetNewsChannelsQuery, List<NewsChannelDto>>
{
    private readonly IApplicationDbContext _context;

    public GetNewsChannelsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<NewsChannelDto>> Handle(GetNewsChannelsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.NewsChannels.AsQueryable();

        if (request.SpaceId.HasValue)
        {
            query = query.Where(c => c.SpaceId == request.SpaceId.Value);
        }

        var channels = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                Channel = c,
                ArticleCount = _context.NewsArticles.Count(a => a.ChannelId == c.Id)
            })
            .ToListAsync(cancellationToken);

        return channels.Select(c => NewsChannelDto.FromEntity(c.Channel, c.ArticleCount)).ToList();
    }
}
