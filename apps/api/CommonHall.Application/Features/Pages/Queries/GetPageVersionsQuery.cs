using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.Pages.Queries;

public sealed record GetPageVersionsQuery(Guid PageId) : IRequest<List<PageVersionDto>>;
