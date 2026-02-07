using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Collections.Queries;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Collections.Handlers;

public sealed class GetCollectionQueryHandler : IRequestHandler<GetCollectionQuery, FileCollectionDto>
{
    private readonly IApplicationDbContext _context;

    public GetCollectionQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FileCollectionDto> Handle(GetCollectionQuery request, CancellationToken cancellationToken)
    {
        var result = await _context.FileCollections
            .Include(c => c.Space)
            .Where(c => c.Id == request.Id)
            .Select(c => new
            {
                Collection = c,
                FileCount = c.Files.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            throw new NotFoundException("FileCollection", request.Id);
        }

        return FileCollectionDto.FromEntity(result.Collection, result.FileCount);
    }
}
