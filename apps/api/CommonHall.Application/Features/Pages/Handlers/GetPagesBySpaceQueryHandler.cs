using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Pages.Queries;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Pages.Handlers;

public sealed class GetPagesBySpaceQueryHandler : IRequestHandler<GetPagesBySpaceQuery, List<PageListDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPagesBySpaceQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PageListDto>> Handle(GetPagesBySpaceQuery request, CancellationToken cancellationToken)
    {
        var spaceExists = await _context.Spaces
            .AnyAsync(s => s.Id == request.SpaceId, cancellationToken);

        if (!spaceExists)
        {
            throw new NotFoundException("Space", request.SpaceId);
        }

        var query = _context.Pages.Where(p => p.SpaceId == request.SpaceId);

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        var pages = await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Title)
            .Select(p => PageListDto.FromEntity(p))
            .ToListAsync(cancellationToken);

        return pages;
    }
}
