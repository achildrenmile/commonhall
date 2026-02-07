using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Pages.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Pages.Handlers;

public sealed class RestorePageVersionCommandHandler : IRequestHandler<RestorePageVersionCommand, PageDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<User> _userManager;

    public RestorePageVersionCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        UserManager<User> userManager)
    {
        _context = context;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<PageDto> Handle(RestorePageVersionCommand request, CancellationToken cancellationToken)
    {
        var page = await _context.Pages
            .Include(p => p.Space)
            .FirstOrDefaultAsync(p => p.Id == request.PageId, cancellationToken);

        if (page is null)
        {
            throw new NotFoundException("Page", request.PageId);
        }

        var versionToRestore = await _context.PageVersions
            .FirstOrDefaultAsync(v => v.Id == request.VersionId && v.PageId == request.PageId, cancellationToken);

        if (versionToRestore is null)
        {
            throw new NotFoundException("PageVersion", request.VersionId);
        }

        // Save current content as new version before restoring (reversible)
        var latestVersionNumber = await _context.PageVersions
            .Where(v => v.PageId == page.Id)
            .MaxAsync(v => (int?)v.VersionNumber, cancellationToken) ?? 0;

        var backupVersion = new PageVersion
        {
            PageId = page.Id,
            Content = page.Content,
            VersionNumber = latestVersionNumber + 1,
            ChangeDescription = $"Auto-saved before restoring version {versionToRestore.VersionNumber}",
            CreatedBy = _currentUser.UserId
        };

        _context.PageVersions.Add(backupVersion);

        // Restore content from version
        page.Content = versionToRestore.Content;
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
