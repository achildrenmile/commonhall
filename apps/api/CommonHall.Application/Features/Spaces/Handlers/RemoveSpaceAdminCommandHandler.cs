using CommonHall.Application.Common;
using CommonHall.Application.Features.Spaces.Commands;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Spaces.Handlers;

public sealed class RemoveSpaceAdminCommandHandler : IRequestHandler<RemoveSpaceAdminCommand>
{
    private readonly IApplicationDbContext _context;

    public RemoveSpaceAdminCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(RemoveSpaceAdminCommand request, CancellationToken cancellationToken)
    {
        var spaceAdmin = await _context.SpaceAdministrators
            .FirstOrDefaultAsync(sa => sa.SpaceId == request.SpaceId && sa.UserId == request.UserId, cancellationToken);

        if (spaceAdmin is null)
        {
            throw new NotFoundException("SpaceAdministrator", $"SpaceId: {request.SpaceId}, UserId: {request.UserId}");
        }

        _context.SpaceAdministrators.Remove(spaceAdmin);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
