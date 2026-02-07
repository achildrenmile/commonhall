using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using CommonHall.Application.UserGroups.Commands;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.UserGroups.Handlers;

public sealed class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, UserGroupDto>
{
    private readonly IApplicationDbContext _context;

    public CreateGroupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserGroupDto> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        var existingGroup = await _context.UserGroups
            .FirstOrDefaultAsync(g => g.Name == request.Name, cancellationToken);

        if (existingGroup is not null)
        {
            throw new ConflictException($"A group with the name '{request.Name}' already exists.");
        }

        var group = new UserGroup
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            RuleDefinition = request.RuleDefinition,
            IsSystem = false
        };

        _context.UserGroups.Add(group);
        await _context.SaveChangesAsync(cancellationToken);

        return UserGroupDto.FromEntity(group, 0);
    }
}
