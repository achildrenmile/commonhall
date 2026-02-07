using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Pages.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Pages.Handlers;

public sealed class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, PageDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<User> _userManager;

    public UpdatePageCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        UserManager<User> userManager)
    {
        _context = context;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<PageDto> Handle(UpdatePageCommand request, CancellationToken cancellationToken)
    {
        var page = await _context.Pages
            .Include(p => p.Space)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (page is null)
        {
            throw new NotFoundException("Page", request.Id);
        }

        // Auto-versioning: save current content as new version before updating
        if (request.Content is not null && request.Content != page.Content)
        {
            var latestVersionNumber = await _context.PageVersions
                .Where(v => v.PageId == page.Id)
                .MaxAsync(v => (int?)v.VersionNumber, cancellationToken) ?? 0;

            var version = new PageVersion
            {
                PageId = page.Id,
                Content = page.Content,
                VersionNumber = latestVersionNumber + 1,
                ChangeDescription = "Auto-saved before update",
                CreatedBy = _currentUser.UserId
            };

            _context.PageVersions.Add(version);
        }

        if (request.Title is not null) page.Title = request.Title;
        if (request.Content is not null) page.Content = request.Content;
        if (request.MetaDescription is not null) page.MetaDescription = request.MetaDescription;
        if (request.VisibilityRule is not null) page.VisibilityRule = request.VisibilityRule;

        page.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        User? creator = null;
        if (page.CreatedBy.HasValue)
        {
            creator = await _userManager.FindByIdAsync(page.CreatedBy.Value.ToString());
        }

        return PageDto.FromEntity(page, creator);
    }
}
