using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.UserGroups.Queries;

public sealed record GetGroupMembersQuery : IRequest<PagedResult<UserDto>>
{
    public Guid GroupId { get; init; }
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
}

public sealed class GetGroupMembersQueryHandler : IRequestHandler<GetGroupMembersQuery, PagedResult<UserDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGroupMembersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<UserDto>> Handle(GetGroupMembersQuery request, CancellationToken cancellationToken)
    {
        var groupExists = await _context.UserGroups
            .AnyAsync(g => g.Id == request.GroupId, cancellationToken);

        if (!groupExists)
        {
            throw new NotFoundException("UserGroup", request.GroupId);
        }

        var query = _context.UserGroupMemberships
            .Where(m => m.UserGroupId == request.GroupId)
            .Select(m => m.User);

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.DisplayName)
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .ToListAsync(cancellationToken);

        var items = users.Select(UserDto.FromEntity).ToList();

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            Size = request.Size
        };
    }
}
