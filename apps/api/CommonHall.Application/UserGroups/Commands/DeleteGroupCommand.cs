using MediatR;

namespace CommonHall.Application.UserGroups.Commands;

public sealed record DeleteGroupCommand(Guid Id) : IRequest;
