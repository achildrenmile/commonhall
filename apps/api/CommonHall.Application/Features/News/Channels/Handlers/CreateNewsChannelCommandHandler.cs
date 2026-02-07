using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Channels.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Channels.Handlers;

public sealed class CreateNewsChannelCommandHandler : IRequestHandler<CreateNewsChannelCommand, NewsChannelDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISlugService _slugService;
    private readonly ICurrentUser _currentUser;

    public CreateNewsChannelCommandHandler(
        IApplicationDbContext context,
        ISlugService slugService,
        ICurrentUser currentUser)
    {
        _context = context;
        _slugService = slugService;
        _currentUser = currentUser;
    }

    public async Task<NewsChannelDto> Handle(CreateNewsChannelCommand request, CancellationToken cancellationToken)
    {
        var spaceExists = await _context.Spaces
            .AnyAsync(s => s.Id == request.SpaceId, cancellationToken);

        if (!spaceExists)
        {
            throw new NotFoundException("Space", request.SpaceId);
        }

        var slug = _slugService.GenerateSlug(request.Name);

        // Check for slug uniqueness within the space
        var slugExists = await _context.NewsChannels
            .AnyAsync(c => c.SpaceId == request.SpaceId && c.Slug == slug, cancellationToken);

        if (slugExists)
        {
            var counter = 2;
            var baseSlug = slug;
            while (await _context.NewsChannels.AnyAsync(c => c.SpaceId == request.SpaceId && c.Slug == slug, cancellationToken))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }
        }

        var maxSortOrder = await _context.NewsChannels
            .Where(c => c.SpaceId == request.SpaceId)
            .MaxAsync(c => (int?)c.SortOrder, cancellationToken) ?? 0;

        var channel = new NewsChannel
        {
            SpaceId = request.SpaceId,
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            Color = request.Color,
            SortOrder = maxSortOrder + 1,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };

        _context.NewsChannels.Add(channel);
        await _context.SaveChangesAsync(cancellationToken);

        return NewsChannelDto.FromEntity(channel, 0);
    }
}
