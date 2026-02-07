using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Channels.Queries;

public sealed record GetNewsChannelByIdQuery(Guid Id) : IRequest<NewsChannelDto>;

public sealed class GetNewsChannelByIdQueryHandler : IRequestHandler<GetNewsChannelByIdQuery, NewsChannelDto>
{
    private readonly IApplicationDbContext _context;

    public GetNewsChannelByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NewsChannelDto> Handle(GetNewsChannelByIdQuery request, CancellationToken cancellationToken)
    {
        var channel = await _context.NewsChannels
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("NewsChannel", request.Id);

        var articleCount = await _context.NewsArticles
            .CountAsync(a => a.ChannelId == channel.Id && !a.IsDeleted, cancellationToken);

        return NewsChannelDto.FromEntity(channel, articleCount);
    }
}
