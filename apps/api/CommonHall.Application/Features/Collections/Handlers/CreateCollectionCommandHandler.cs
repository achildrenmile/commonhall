using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Collections.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Collections.Handlers;

public sealed class CreateCollectionCommandHandler : IRequestHandler<CreateCollectionCommand, FileCollectionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateCollectionCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<FileCollectionDto> Handle(CreateCollectionCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AuthenticationException("UNAUTHORIZED", "User is not authenticated.");
        }

        // Validate space if provided
        if (request.SpaceId.HasValue)
        {
            var spaceExists = await _context.Spaces
                .AnyAsync(s => s.Id == request.SpaceId.Value, cancellationToken);

            if (!spaceExists)
            {
                throw new NotFoundException("Space", request.SpaceId.Value);
            }
        }

        var collection = new FileCollection
        {
            Name = request.Name,
            Description = request.Description,
            SpaceId = request.SpaceId,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };

        _context.FileCollections.Add(collection);
        await _context.SaveChangesAsync(cancellationToken);

        // Load space if exists
        if (request.SpaceId.HasValue)
        {
            collection.Space = await _context.Spaces
                .FirstOrDefaultAsync(s => s.Id == request.SpaceId.Value, cancellationToken);
        }

        return FileCollectionDto.FromEntity(collection, 0);
    }
}
