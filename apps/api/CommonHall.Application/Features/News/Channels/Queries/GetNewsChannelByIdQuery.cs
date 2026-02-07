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

        return new NewsChannelDto
        {
            Id = channel.Id,
            SpaceId = channel.SpaceId,
            Name = channel.Name,
            Slug = channel.Slug,
            Description = channel.Description,
            Color = channel.Color,
            SortOrder = channel.SortOrder,
            CreatedAt = channel.CreatedAt
        };
    }
}
