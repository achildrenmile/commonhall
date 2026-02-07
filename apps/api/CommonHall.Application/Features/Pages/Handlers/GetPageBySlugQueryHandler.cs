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
    private readonly ICurrentUser _currentUser;
    private readonly ITargetingService _targetingService;

    public GetPageBySlugQueryHandler(
        IApplicationDbContext context,
        UserManager<User> userManager,
        ICurrentUser currentUser,
        ITargetingService targetingService)
    {
        _context = context;
        _userManager = userManager;
        _currentUser = currentUser;
        _targetingService = targetingService;
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

        // Check page-level visibility
        if (_currentUser.UserId.HasValue)
        {
            var isVisible = await _targetingService.IsVisibleAsync(
                _currentUser.UserId.Value,
                page.VisibilityRule,
                cancellationToken);

            if (!isVisible)
            {
                throw new ForbiddenException("You do not have access to this page.");
            }

            // Filter widgets based on visibility rules
            var filteredContent = await _targetingService.FilterWidgetsAsync(
                page.Content,
                _currentUser.UserId.Value,
                cancellationToken);
            page.Content = filteredContent;
        }
        else if (!string.IsNullOrEmpty(page.VisibilityRule))
        {
            // Anonymous users cannot access targeted pages
            throw new ForbiddenException("You must be logged in to access this page.");
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
