using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.Files.Queries;

public sealed record GetFileQuery(Guid Id) : IRequest<StoredFileDto>;
