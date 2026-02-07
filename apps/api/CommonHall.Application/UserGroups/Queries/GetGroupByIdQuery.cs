using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.UserGroups.Queries;

public sealed record GetGroupByIdQuery(Guid Id) : IRequest<UserGroupDto>;

public sealed class GetGroupByIdQueryHandler : IRequestHandler<GetGroupByIdQuery, UserGroupDto>
{
    private readonly IApplicationDbContext _context;

    public GetGroupByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserGroupDto> Handle(GetGroupByIdQuery request, CancellationToken cancellationToken)
    {
        var group = await _context.UserGroups
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (group is null)
        {
            throw new NotFoundException("UserGroup", request.Id);
        }

        var memberCount = await _context.UserGroupMemberships
            .CountAsync(m => m.UserGroupId == group.Id, cancellationToken);

        return UserGroupDto.FromEntity(group, memberCount);
    }
}
