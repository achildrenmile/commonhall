using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Users.Queries;

public sealed record GetUserGroupsQuery(Guid UserId) : IRequest<List<UserGroupDto>>;

public sealed class GetUserGroupsQueryHandler : IRequestHandler<GetUserGroupsQuery, List<UserGroupDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserGroupsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserGroupDto>> Handle(GetUserGroupsQuery request, CancellationToken cancellationToken)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            throw new NotFoundException("User", request.UserId);
        }

        var groups = await _context.UserGroupMemberships
            .Where(m => m.UserId == request.UserId)
            .Include(m => m.UserGroup)
            .Select(m => new
            {
                Group = m.UserGroup,
                MemberCount = _context.UserGroupMemberships.Count(gm => gm.UserGroupId == m.UserGroupId)
            })
            .ToListAsync(cancellationToken);

        return groups.Select(g => UserGroupDto.FromEntity(g.Group, g.MemberCount)).ToList();
    }
}
