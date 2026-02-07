using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Collections.Queries;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Collections.Handlers;

public sealed class ListCollectionsQueryHandler : IRequestHandler<ListCollectionsQuery, List<FileCollectionDto>>
{
    private readonly IApplicationDbContext _context;

    public ListCollectionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FileCollectionDto>> Handle(ListCollectionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.FileCollections
            .Include(c => c.Space)
            .AsQueryable();

        // Filter by space
        if (request.SpaceId.HasValue)
        {
            query = query.Where(c => c.SpaceId == request.SpaceId.Value);
        }

        // Filter by search term
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(searchLower));
        }

        var collections = await query
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                Collection = c,
                FileCount = c.Files.Count
            })
            .ToListAsync(cancellationToken);

        return collections
            .Select(x => FileCollectionDto.FromEntity(x.Collection, x.FileCount))
            .ToList();
    }
}
