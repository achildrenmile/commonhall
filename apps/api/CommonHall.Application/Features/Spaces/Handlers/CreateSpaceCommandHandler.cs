using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Spaces.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Spaces.Handlers;

public sealed class CreateSpaceCommandHandler : IRequestHandler<CreateSpaceCommand, SpaceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISlugService _slugService;
    private readonly ICurrentUser _currentUser;

    public CreateSpaceCommandHandler(
        IApplicationDbContext context,
        ISlugService slugService,
        ICurrentUser currentUser)
    {
        _context = context;
        _slugService = slugService;
        _currentUser = currentUser;
    }

    public async Task<SpaceDto> Handle(CreateSpaceCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentSpaceId.HasValue)
        {
            var parentExists = await _context.Spaces
                .AnyAsync(s => s.Id == request.ParentSpaceId.Value, cancellationToken);

            if (!parentExists)
            {
                throw new NotFoundException("Space", request.ParentSpaceId.Value);
            }
        }

        var slug = await _slugService.GenerateUniqueSpaceSlugAsync(request.Name, cancellationToken);

        var maxSortOrder = await _context.Spaces
            .Where(s => s.ParentSpaceId == request.ParentSpaceId)
            .MaxAsync(s => (int?)s.SortOrder, cancellationToken) ?? 0;

        var space = new Space
        {
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            IconUrl = request.IconUrl,
            ParentSpaceId = request.ParentSpaceId,
            SortOrder = maxSortOrder + 1,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };

        _context.Spaces.Add(space);
        await _context.SaveChangesAsync(cancellationToken);

        return SpaceDto.FromEntity(space, 0);
    }
}
