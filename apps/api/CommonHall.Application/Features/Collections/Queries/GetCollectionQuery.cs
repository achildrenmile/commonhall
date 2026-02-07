using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.Collections.Queries;

public sealed record GetCollectionQuery(Guid Id) : IRequest<FileCollectionDto>;
