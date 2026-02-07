using CommonHall.Application.Common;
using CommonHall.Application.Interfaces;
using CommonHall.Application.UserGroups.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.UserGroups.Handlers;

public sealed class RemoveGroupMemberCommandHandler : IRequestHandler<RemoveGroupMemberCommand>
{
    private readonly IApplicationDbContext _context;

    public RemoveGroupMemberCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(RemoveGroupMemberCommand request, CancellationToken cancellationToken)
    {
        var membership = await _context.UserGroupMemberships
            .FirstOrDefaultAsync(m => m.UserGroupId == request.GroupId && m.UserId == request.UserId, cancellationToken);

        if (membership is null)
        {
            throw new NotFoundException($"Membership for user {request.UserId} in group {request.GroupId}");
        }

        _context.UserGroupMemberships.Remove(membership);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
