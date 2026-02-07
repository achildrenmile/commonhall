using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Channels.Commands;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Channels.Handlers;

public sealed class UpdateNewsChannelCommandHandler : IRequestHandler<UpdateNewsChannelCommand, NewsChannelDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public UpdateNewsChannelCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<NewsChannelDto> Handle(UpdateNewsChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await _context.NewsChannels
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (channel is null)
        {
            throw new NotFoundException("NewsChannel", request.Id);
        }

        if (request.Name is not null) channel.Name = request.Name;
        if (request.Description is not null) channel.Description = request.Description;
        if (request.Color is not null) channel.Color = request.Color;
        if (request.SortOrder.HasValue) channel.SortOrder = request.SortOrder.Value;

        channel.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        var articleCount = await _context.NewsArticles
            .CountAsync(a => a.ChannelId == channel.Id, cancellationToken);

        return NewsChannelDto.FromEntity(channel, articleCount);
    }
}
