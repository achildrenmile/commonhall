using CommonHall.Application.Common;
using CommonHall.Application.Features.Spaces.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Spaces.Handlers;

public sealed class AddSpaceAdminCommandHandler : IRequestHandler<AddSpaceAdminCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public AddSpaceAdminCommandHandler(IApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task Handle(AddSpaceAdminCommand request, CancellationToken cancellationToken)
    {
        var spaceExists = await _context.Spaces
            .AnyAsync(s => s.Id == request.SpaceId, cancellationToken);

        if (!spaceExists)
        {
            throw new NotFoundException("Space", request.SpaceId);
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        var alreadyAdmin = await _context.SpaceAdministrators
            .AnyAsync(sa => sa.SpaceId == request.SpaceId && sa.UserId == request.UserId, cancellationToken);

        if (alreadyAdmin)
        {
            throw new ConflictException("User is already an administrator of this space.");
        }

        var spaceAdmin = new SpaceAdministrator
        {
            SpaceId = request.SpaceId,
            UserId = request.UserId
        };

        _context.SpaceAdministrators.Add(spaceAdmin);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
