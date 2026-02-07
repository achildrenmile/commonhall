using CommonHall.Application.Common;
using CommonHall.Application.Features.Pages.Commands;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Pages.Handlers;

public sealed class SetHomePageCommandHandler : IRequestHandler<SetHomePageCommand>
{
    private readonly IApplicationDbContext _context;

    public SetHomePageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(SetHomePageCommand request, CancellationToken cancellationToken)
    {
        var spaceExists = await _context.Spaces
            .AnyAsync(s => s.Id == request.SpaceId, cancellationToken);

        if (!spaceExists)
        {
            throw new NotFoundException("Space", request.SpaceId);
        }

        var page = await _context.Pages
            .FirstOrDefaultAsync(p => p.Id == request.PageId && p.SpaceId == request.SpaceId, cancellationToken);

        if (page is null)
        {
            throw new NotFoundException("Page", request.PageId);
        }

        // Remove home page flag from all other pages in the space
        var currentHomePages = await _context.Pages
            .Where(p => p.SpaceId == request.SpaceId && p.IsHomePage && p.Id != request.PageId)
            .ToListAsync(cancellationToken);

        foreach (var homePage in currentHomePages)
        {
            homePage.IsHomePage = false;
        }

        page.IsHomePage = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
