using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Spaces.Commands;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Spaces.Handlers;

public sealed class UpdateSpaceCommandHandler : IRequestHandler<UpdateSpaceCommand, SpaceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public UpdateSpaceCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<SpaceDto> Handle(UpdateSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await _context.Spaces
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (space is null)
        {
            throw new NotFoundException("Space", request.Id);
        }

        if (request.Name is not null) space.Name = request.Name;
        if (request.Description is not null) space.Description = request.Description;
        if (request.IconUrl is not null) space.IconUrl = request.IconUrl;
        if (request.CoverImageUrl is not null) space.CoverImageUrl = request.CoverImageUrl;
        if (request.SortOrder.HasValue) space.SortOrder = request.SortOrder.Value;

        space.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        var pageCount = await _context.Pages
            .CountAsync(p => p.SpaceId == space.Id, cancellationToken);

        return SpaceDto.FromEntity(space, pageCount);
    }
}
