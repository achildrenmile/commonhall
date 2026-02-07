using CommonHall.Application.Common;
using CommonHall.Application.Interfaces;
using CommonHall.Application.UserGroups.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.UserGroups.Handlers;

public sealed class DeleteGroupCommandHandler : IRequestHandler<DeleteGroupCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteGroupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _context.UserGroups
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (group is null)
        {
            throw new NotFoundException("UserGroup", request.Id);
        }

        if (group.IsSystem)
        {
            throw new ForbiddenException("System groups cannot be deleted.");
        }

        _context.UserGroups.Remove(group);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
