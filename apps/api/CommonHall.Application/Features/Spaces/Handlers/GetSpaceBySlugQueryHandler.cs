using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Spaces.Queries;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Spaces.Handlers;

public sealed class GetSpaceBySlugQueryHandler : IRequestHandler<GetSpaceBySlugQuery, SpaceDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public GetSpaceBySlugQueryHandler(IApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<SpaceDetailDto> Handle(GetSpaceBySlugQuery request, CancellationToken cancellationToken)
    {
        var space = await _context.Spaces
            .FirstOrDefaultAsync(s => s.Slug == request.Slug, cancellationToken);

        if (space is null)
        {
            throw new NotFoundException("Space", request.Slug);
        }

        // Get pages
        var pages = await _context.Pages
            .Where(p => p.SpaceId == space.Id)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Title)
            .Select(p => PageListDto.FromEntity(p))
            .ToListAsync(cancellationToken);

        // Get admins
        var adminIds = await _context.SpaceAdministrators
            .Where(sa => sa.SpaceId == space.Id)
            .Select(sa => sa.UserId)
            .ToListAsync(cancellationToken);

        var admins = new List<UserSummaryDto>();
        foreach (var adminId in adminIds)
        {
            var user = await _userManager.FindByIdAsync(adminId.ToString());
            if (user is not null)
            {
                admins.Add(UserSummaryDto.FromEntity(user));
            }
        }

        // Get child spaces
        var childSpaces = await _context.Spaces
            .Where(s => s.ParentSpaceId == space.Id)
            .OrderBy(s => s.SortOrder)
            .Select(s => new
            {
                Space = s,
                PageCount = _context.Pages.Count(p => p.SpaceId == s.Id)
            })
            .ToListAsync(cancellationToken);

        var childSpaceDtos = childSpaces.Select(s => SpaceDto.FromEntity(s.Space, s.PageCount)).ToList();

        return SpaceDetailDto.FromEntity(space, pages, admins, childSpaceDtos);
    }
}
