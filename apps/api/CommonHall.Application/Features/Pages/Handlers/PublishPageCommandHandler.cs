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

public sealed class PublishPageCommandHandler : IRequestHandler<PublishPageCommand, PageDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<User> _userManager;

    public PublishPageCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        UserManager<User> userManager)
    {
        _context = context;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<PageDto> Handle(PublishPageCommand request, CancellationToken cancellationToken)
    {
        var page = await _context.Pages
            .Include(p => p.Space)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (page is null)
        {
            throw new NotFoundException("Page", request.Id);
        }

        page.Status = ContentStatus.Published;
        page.PublishedAt = DateTimeOffset.UtcNow;
        page.PublishedBy = _currentUser.UserId;
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
