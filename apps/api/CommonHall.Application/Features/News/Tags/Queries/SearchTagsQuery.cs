using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.News.Tags.Queries;

public sealed record SearchTagsQuery : IRequest<List<TagDto>>
{
    public string? Search { get; init; }
    public int Limit { get; init; } = 20;
}
