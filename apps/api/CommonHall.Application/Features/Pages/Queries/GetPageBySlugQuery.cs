using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.Pages.Queries;

public sealed record GetPageBySlugQuery(string SpaceSlug, string PageSlug) : IRequest<PageDetailDto>;
