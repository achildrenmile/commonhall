using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Collections.Commands;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Collections.Handlers;

public sealed class UpdateCollectionCommandHandler : IRequestHandler<UpdateCollectionCommand, FileCollectionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public UpdateCollectionCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<FileCollectionDto> Handle(UpdateCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _context.FileCollections
            .Include(c => c.Space)
            .Include(c => c.Files)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (collection is null)
        {
            throw new NotFoundException("FileCollection", request.Id);
        }

        if (request.Name != null)
        {
            collection.Name = request.Name;
        }

        if (request.Description != null)
        {
            collection.Description = request.Description;
        }

        collection.UpdatedBy = _currentUser.UserId;
        await _context.SaveChangesAsync(cancellationToken);

        return FileCollectionDto.FromEntity(collection, collection.Files.Count);
    }
}
