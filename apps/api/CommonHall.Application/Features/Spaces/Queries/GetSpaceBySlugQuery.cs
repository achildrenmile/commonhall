using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.Spaces.Queries;

public sealed record GetSpaceBySlugQuery(string Slug) : IRequest<SpaceDetailDto>;
