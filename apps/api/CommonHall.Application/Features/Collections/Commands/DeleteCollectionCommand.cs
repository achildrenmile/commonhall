using MediatR;

namespace CommonHall.Application.Features.Collections.Commands;

public sealed record DeleteCollectionCommand(Guid Id) : IRequest;
