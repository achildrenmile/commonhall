using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Users.Queries;

public sealed record FacetItem(string Name, int Count);

public sealed record SearchFacets
{
    public IReadOnlyList<FacetItem> Departments { get; init; } = [];
    public IReadOnlyList<FacetItem> Locations { get; init; } = [];
}

public sealed record SearchUsersResult
{
    public required IReadOnlyList<UserDto> Items { get; init; }
    public required int TotalCount { get; init; }
    public required bool HasNextPage { get; init; }
    public string? NextCursor { get; init; }
    public required SearchFacets Facets { get; init; }
}

public sealed record SearchUsersQuery : IRequest<SearchUsersResult>
{
    public string? Query { get; init; }
    public IReadOnlyList<string>? Departments { get; init; }
    public IReadOnlyList<string>? Locations { get; init; }
    public string? Cursor { get; init; }
    public int Size { get; init; } = 20;
}

public sealed class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, SearchUsersResult>
{
    private readonly IApplicationDbContext _context;

    public SearchUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SearchUsersResult> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = _context.Users
            .Where(u => u.IsActive && !u.IsDeleted)
            .AsQueryable();

        // Build facets from unfiltered query (for accurate counts)
        var facetsQuery = baseQuery;

        var departmentFacets = await facetsQuery
            .Where(u => u.Department != null)
            .GroupBy(u => u.Department!)
            .Select(g => new FacetItem(g.Key, g.Count()))
            .OrderByDescending(f => f.Count)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken);

        var locationFacets = await facetsQuery
            .Where(u => u.Location != null)
            .GroupBy(u => u.Location!)
            .Select(g => new FacetItem(g.Key, g.Count()))
            .OrderByDescending(f => f.Count)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken);

        // Apply filters
        var query = baseQuery;

        if (request.Departments is { Count: > 0 })
        {
            query = query.Where(u => u.Department != null && request.Departments.Contains(u.Department));
        }

        if (request.Locations is { Count: > 0 })
        {
            query = query.Where(u => u.Location != null && request.Locations.Contains(u.Location));
        }

        // Apply search
        bool hasSearch = !string.IsNullOrWhiteSpace(request.Query);

        if (hasSearch)
        {
            var searchTerms = request.Query!.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var term in searchTerms)
            {
                query = query.Where(u =>
                    EF.Functions.ILike(u.Email!, $"%{term}%") ||
                    EF.Functions.ILike(u.DisplayName, $"%{term}%") ||
                    (u.FirstName != null && EF.Functions.ILike(u.FirstName, $"%{term}%")) ||
                    (u.LastName != null && EF.Functions.ILike(u.LastName, $"%{term}%")) ||
                    (u.JobTitle != null && EF.Functions.ILike(u.JobTitle, $"%{term}%")) ||
                    (u.Department != null && EF.Functions.ILike(u.Department, $"%{term}%")));
            }
        }

        // Apply ordering: relevance when searching, alphabetical when browsing
        if (hasSearch)
        {
            // For relevance, prioritize exact matches and full name matches
            var searchLower = request.Query!.ToLower();
            query = query.OrderByDescending(u =>
                    EF.Functions.ILike(u.DisplayName, $"{searchLower}%") ? 3 :
                    EF.Functions.ILike(u.DisplayName, $"%{searchLower}%") ? 2 :
                    EF.Functions.ILike(u.Email!, $"%{searchLower}%") ? 1 : 0)
                .ThenBy(u => u.DisplayName);
        }
        else
        {
            query = query.OrderBy(u => u.DisplayName);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply cursor-based pagination
        if (!string.IsNullOrEmpty(request.Cursor))
        {
            var cursorName = DecodeCursor(request.Cursor);
            if (cursorName != null)
            {
                query = query.Where(u => string.Compare(u.DisplayName, cursorName) > 0);
            }
        }

        var users = await query
            .Take(request.Size + 1) // Fetch one extra to check for next page
            .ToListAsync(cancellationToken);

        var hasNextPage = users.Count > request.Size;
        var resultUsers = users.Take(request.Size).ToList();
        var nextCursor = hasNextPage && resultUsers.Count > 0
            ? EncodeCursor(resultUsers.Last().DisplayName)
            : null;

        return new SearchUsersResult
        {
            Items = resultUsers.Select(UserDto.FromEntity).ToList(),
            TotalCount = totalCount,
            HasNextPage = hasNextPage,
            NextCursor = nextCursor,
            Facets = new SearchFacets
            {
                Departments = departmentFacets,
                Locations = locationFacets
            }
        };
    }

    private static string EncodeCursor(string displayName)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(displayName));
    }

    private static string? DecodeCursor(string cursor)
    {
        try
        {
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        }
        catch
        {
            return null;
        }
    }
}
