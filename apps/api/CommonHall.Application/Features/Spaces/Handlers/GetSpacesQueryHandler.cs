using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Spaces.Queries;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Spaces.Handlers;

public sealed class GetSpacesQueryHandler : IRequestHandler<GetSpacesQuery, List<SpaceDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSpacesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SpaceDto>> Handle(GetSpacesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Spaces.AsQueryable();

        if (request.ParentSpaceId.HasValue)
        {
            query = query.Where(s => s.ParentSpaceId == request.ParentSpaceId.Value);
        }
        else if (!request.IncludeChildren)
        {
            // Only root spaces (no parent)
            query = query.Where(s => s.ParentSpaceId == null);
        }

        var spaces = await query
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .Select(s => new
            {
                Space = s,
                PageCount = _context.Pages.Count(p => p.SpaceId == s.Id)
            })
            .ToListAsync(cancellationToken);

        return spaces.Select(s => SpaceDto.FromEntity(s.Space, s.PageCount)).ToList();
    }
}
