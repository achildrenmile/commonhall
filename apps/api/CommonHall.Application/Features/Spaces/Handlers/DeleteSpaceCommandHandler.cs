using CommonHall.Application.Common;
using CommonHall.Application.Features.Spaces.Commands;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Spaces.Handlers;

public sealed class DeleteSpaceCommandHandler : IRequestHandler<DeleteSpaceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeleteSpaceCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await _context.Spaces
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (space is null)
        {
            throw new NotFoundException("Space", request.Id);
        }

        if (space.IsDefault)
        {
            throw new ForbiddenException("Cannot delete the default space.");
        }

        var hasChildSpaces = await _context.Spaces
            .AnyAsync(s => s.ParentSpaceId == request.Id, cancellationToken);

        if (hasChildSpaces)
        {
            throw new ConflictException("Cannot delete a space that has child spaces. Delete child spaces first.");
        }

        // Soft delete
        space.IsDeleted = true;
        space.DeletedAt = DateTimeOffset.UtcNow;
        space.DeletedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
