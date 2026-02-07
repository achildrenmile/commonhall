using CommonHall.Application.Common;
using CommonHall.Application.Features.News.Channels.Commands;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Channels.Handlers;

public sealed class DeleteNewsChannelCommandHandler : IRequestHandler<DeleteNewsChannelCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteNewsChannelCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteNewsChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await _context.NewsChannels
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (channel is null)
        {
            throw new NotFoundException("NewsChannel", request.Id);
        }

        // Check if channel has articles
        var hasArticles = await _context.NewsArticles
            .AnyAsync(a => a.ChannelId == request.Id, cancellationToken);

        if (hasArticles)
        {
            throw new ConflictException("Cannot delete a channel that has articles. Move or delete articles first.");
        }

        _context.NewsChannels.Remove(channel);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
