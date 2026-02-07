using CommonHall.Application.Common;
using CommonHall.Application.Features.Pages.Commands;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Pages.Handlers;

public sealed class ReorderPagesCommandHandler : IRequestHandler<ReorderPagesCommand>
{
    private readonly IApplicationDbContext _context;

    public ReorderPagesCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ReorderPagesCommand request, CancellationToken cancellationToken)
    {
        var spaceExists = await _context.Spaces
            .AnyAsync(s => s.Id == request.SpaceId, cancellationToken);

        if (!spaceExists)
        {
            throw new NotFoundException("Space", request.SpaceId);
        }

        var pageIds = request.Pages.Select(p => p.Id).ToList();
        var pages = await _context.Pages
            .Where(p => p.SpaceId == request.SpaceId && pageIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var orderItem in request.Pages)
        {
            var page = pages.FirstOrDefault(p => p.Id == orderItem.Id);
            if (page is not null)
            {
                page.SortOrder = orderItem.SortOrder;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
