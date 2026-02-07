using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.Pages.Commands;

public sealed record UnpublishPageCommand(Guid Id) : IRequest<PageDto>;
