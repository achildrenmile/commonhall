using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using CommonHall.Application.UserGroups.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.UserGroups.Handlers;

public sealed class UpdateGroupCommandHandler : IRequestHandler<UpdateGroupCommand, UserGroupDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateGroupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserGroupDto> Handle(UpdateGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _context.UserGroups
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (group is null)
        {
            throw new NotFoundException("UserGroup", request.Id);
        }

        if (request.Name is not null && request.Name != group.Name)
        {
            var existingGroup = await _context.UserGroups
                .FirstOrDefaultAsync(g => g.Name == request.Name && g.Id != request.Id, cancellationToken);

            if (existingGroup is not null)
            {
                throw new ConflictException($"A group with the name '{request.Name}' already exists.");
            }

            group.Name = request.Name;
        }

        if (request.Description is not null) group.Description = request.Description;
        if (request.RuleDefinition is not null) group.RuleDefinition = request.RuleDefinition;

        await _context.SaveChangesAsync(cancellationToken);

        var memberCount = await _context.UserGroupMemberships
            .CountAsync(m => m.UserGroupId == group.Id, cancellationToken);

        return UserGroupDto.FromEntity(group, memberCount);
    }
}
