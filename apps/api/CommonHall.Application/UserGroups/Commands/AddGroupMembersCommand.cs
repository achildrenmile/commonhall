using MediatR;

namespace CommonHall.Application.UserGroups.Commands;

public sealed record AddGroupMembersCommand : IRequest
{
    public Guid GroupId { get; init; }
    public required List<Guid> UserIds { get; init; }
}
