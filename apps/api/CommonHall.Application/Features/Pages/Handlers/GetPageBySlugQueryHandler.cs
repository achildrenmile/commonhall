using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Pages.Queries;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Pages.Handlers;

public sealed class GetPageBySlugQueryHandler : IRequestHandler<GetPageBySlugQuery, PageDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public GetPageBySlugQueryHandler(IApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<PageDetailDto> Handle(GetPageBySlugQuery request, CancellationToken cancellationToken)
    {
        var space = await _context.Spaces
            .FirstOrDefaultAsync(s => s.Slug == request.SpaceSlug, cancellationToken);

        if (space is null)
        {
            throw new NotFoundException("Space", request.SpaceSlug);
        }

        var page = await _context.Pages
            .Include(p => p.Space)
            .FirstOrDefaultAsync(p => p.SpaceId == space.Id && p.Slug == request.PageSlug, cancellationToken);

        if (page is null)
        {
            throw new NotFoundException("Page", request.PageSlug);
        }

        User? creator = null;
        if (page.CreatedBy.HasValue)
        {
            creator = await _userManager.FindByIdAsync(page.CreatedBy.Value.ToString());
        }

        var versionCount = await _context.PageVersions
            .CountAsync(v => v.PageId == page.Id, cancellationToken);

        return PageDetailDto.FromEntity(page, creator, versionCount);
    }
}
