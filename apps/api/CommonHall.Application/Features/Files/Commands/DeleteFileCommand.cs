using MediatR;

namespace CommonHall.Application.Features.Files.Commands;

public sealed record DeleteFileCommand(Guid Id) : IRequest;
