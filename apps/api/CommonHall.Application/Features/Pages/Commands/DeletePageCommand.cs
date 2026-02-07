using MediatR;

namespace CommonHall.Application.Features.Pages.Commands;

public sealed record DeletePageCommand(Guid Id) : IRequest;
