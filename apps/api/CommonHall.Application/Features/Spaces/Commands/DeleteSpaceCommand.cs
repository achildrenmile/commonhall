using MediatR;

namespace CommonHall.Application.Features.Spaces.Commands;

public sealed record DeleteSpaceCommand(Guid Id) : IRequest;
