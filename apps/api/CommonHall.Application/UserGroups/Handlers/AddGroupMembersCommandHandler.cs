using CommonHall.Application.Common;
using CommonHall.Application.Interfaces;
using CommonHall.Application.UserGroups.Commands;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.UserGroups.Handlers;

public sealed class AddGroupMembersCommandHandler : IRequestHandler<AddGroupMembersCommand>
{
    private readonly IApplicationDbContext _context;

    public AddGroupMembersCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(AddGroupMembersCommand request, CancellationToken cancellationToken)
    {
        var group = await _context.UserGroups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group is null)
        {
            throw new NotFoundException("UserGroup", request.GroupId);
        }

        var existingMemberIds = await _context.UserGroupMemberships
            .Where(m => m.UserGroupId == request.GroupId && request.UserIds.Contains(m.UserId))
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var newUserIds = request.UserIds.Except(existingMemberIds).ToList();

        foreach (var userId in newUserIds)
        {
            _context.UserGroupMemberships.Add(new UserGroupMembership
            {
                UserGroupId = request.GroupId,
                UserId = userId,
                JoinedAt = DateTimeOffset.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
