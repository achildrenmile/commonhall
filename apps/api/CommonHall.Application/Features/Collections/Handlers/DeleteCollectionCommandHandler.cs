using CommonHall.Application.Common;
using CommonHall.Application.Features.Collections.Commands;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Collections.Handlers;

public sealed class DeleteCollectionCommandHandler : IRequestHandler<DeleteCollectionCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteCollectionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _context.FileCollections
            .Include(c => c.Files)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (collection is null)
        {
            throw new NotFoundException("FileCollection", request.Id);
        }

        // Check if collection has files
        if (collection.Files.Count > 0)
        {
            throw new ConflictException("Cannot delete collection that contains files. Move or delete the files first.");
        }

        _context.FileCollections.Remove(collection);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
