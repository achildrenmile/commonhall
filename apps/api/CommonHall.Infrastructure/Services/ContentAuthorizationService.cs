using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Infrastructure.Services;

public sealed class ContentAuthorizationService : IContentAuthorizationService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public ContentAuthorizationService(IApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<bool> CanManageSpaceAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return false;

        // Admin can manage any space
        if (user.Role == UserRole.Admin)
            return true;

        // Space admin can manage their space
        return await IsSpaceAdminAsync(userId, spaceId, cancellationToken);
    }

    public async Task<bool> CanEditContentAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return false;

        // Admin can edit any content
        if (user.Role == UserRole.Admin)
            return true;

        // Editor can edit content in any space
        if (user.Role == UserRole.Editor)
            return true;

        // Space admin can edit content in their space
        return await IsSpaceAdminAsync(userId, spaceId, cancellationToken);
    }

    public async Task<bool> IsSpaceAdminAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken = default)
    {
        return await _context.SpaceAdministrators
            .AnyAsync(sa => sa.SpaceId == spaceId && sa.UserId == userId, cancellationToken);
    }

    public async Task<bool> IsPageCreatorAsync(Guid userId, Guid pageId, CancellationToken cancellationToken = default)
    {
        return await _context.Pages
            .AnyAsync(p => p.Id == pageId && p.CreatedBy == userId, cancellationToken);
    }
}
