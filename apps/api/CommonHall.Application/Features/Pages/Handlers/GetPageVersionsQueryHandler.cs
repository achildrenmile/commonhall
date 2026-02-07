using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Pages.Queries;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Pages.Handlers;

public sealed class GetPageVersionsQueryHandler : IRequestHandler<GetPageVersionsQuery, List<PageVersionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public GetPageVersionsQueryHandler(IApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<PageVersionDto>> Handle(GetPageVersionsQuery request, CancellationToken cancellationToken)
    {
        var pageExists = await _context.Pages
            .AnyAsync(p => p.Id == request.PageId, cancellationToken);

        if (!pageExists)
        {
            throw new NotFoundException("Page", request.PageId);
        }

        var versions = await _context.PageVersions
            .Where(v => v.PageId == request.PageId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        var result = new List<PageVersionDto>();
        foreach (var version in versions)
        {
            User? creator = null;
            if (version.CreatedBy.HasValue)
            {
                creator = await _userManager.FindByIdAsync(version.CreatedBy.Value.ToString());
            }
            result.Add(PageVersionDto.FromEntity(version, creator));
        }

        return result;
    }
}
