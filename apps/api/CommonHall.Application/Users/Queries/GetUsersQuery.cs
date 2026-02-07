using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Users.Queries;

public sealed record GetUsersQuery : IRequest<PagedResult<UserDto>>
{
    public string? Search { get; init; }
    public string? Department { get; init; }
    public string? Location { get; init; }
    public UserRole? Role { get; init; }
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
}

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(u =>
                u.Email!.ToLower().Contains(search) ||
                u.DisplayName.ToLower().Contains(search) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(search)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            query = query.Where(u => u.Department == request.Department);
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            query = query.Where(u => u.Location == request.Location);
        }

        if (request.Role.HasValue)
        {
            query = query.Where(u => u.Role == request.Role.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.DisplayName)
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>
        {
            Items = users.Select(UserDto.FromEntity).ToList(),
            TotalCount = total,
            Page = request.Page,
            Size = request.Size
        };
    }
}
