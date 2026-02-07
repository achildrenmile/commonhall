using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Pages.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Pages.Handlers;

public sealed class CreatePageCommandHandler : IRequestHandler<CreatePageCommand, PageDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISlugService _slugService;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<User> _userManager;

    public CreatePageCommandHandler(
        IApplicationDbContext context,
        ISlugService slugService,
        ICurrentUser currentUser,
        UserManager<User> userManager)
    {
        _context = context;
        _slugService = slugService;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<PageDto> Handle(CreatePageCommand request, CancellationToken cancellationToken)
    {
        var space = await _context.Spaces
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId, cancellationToken);

        if (space is null)
        {
            throw new NotFoundException("Space", request.SpaceId);
        }

        var slug = await _slugService.GenerateUniquePageSlugAsync(request.SpaceId, request.Title, cancellationToken);

        var maxSortOrder = await _context.Pages
            .Where(p => p.SpaceId == request.SpaceId)
            .MaxAsync(p => (int?)p.SortOrder, cancellationToken) ?? 0;

        var page = new Page
        {
            SpaceId = request.SpaceId,
            Title = request.Title,
            Slug = slug,
            Content = request.Content,
            Status = request.Status,
            VisibilityRule = request.VisibilityRule,
            SortOrder = maxSortOrder + 1,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };

        if (request.Status == ContentStatus.Published)
        {
            page.PublishedAt = DateTimeOffset.UtcNow;
            page.PublishedBy = _currentUser.UserId;
        }

        _context.Pages.Add(page);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with space
        page.Space = space;

        User? creator = null;
        if (_currentUser.UserId.HasValue)
        {
            creator = await _userManager.FindByIdAsync(_currentUser.UserId.Value.ToString());
        }

        return PageDto.FromEntity(page, creator);
    }
}
