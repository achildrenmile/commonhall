using MediatR;

namespace CommonHall.Application.UserGroups.Commands;

public sealed record RemoveGroupMemberCommand(Guid GroupId, Guid UserId) : IRequest;
