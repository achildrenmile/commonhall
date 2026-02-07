using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.UserGroups.Queries;

public sealed record GetGroupsQuery : IRequest<PagedResult<UserGroupDto>>
{
    public string? Search { get; init; }
    public GroupType? Type { get; init; }
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
}

public sealed class GetGroupsQueryHandler : IRequestHandler<GetGroupsQuery, PagedResult<UserGroupDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGroupsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<UserGroupDto>> Handle(GetGroupsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.UserGroups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(g =>
                g.Name.ToLower().Contains(search) ||
                (g.Description != null && g.Description.ToLower().Contains(search)));
        }

        if (request.Type.HasValue)
        {
            query = query.Where(g => g.Type == request.Type.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var groups = await query
            .OrderBy(g => g.Name)
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .Select(g => new
            {
                Group = g,
                MemberCount = _context.UserGroupMemberships.Count(m => m.UserGroupId == g.Id)
            })
            .ToListAsync(cancellationToken);

        var items = groups.Select(g => UserGroupDto.FromEntity(g.Group, g.MemberCount)).ToList();

        return new PagedResult<UserGroupDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            Size = request.Size
        };
    }
}
