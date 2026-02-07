using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Domain.Enums;
using MediatR;

namespace CommonHall.Application.Features.News.Articles.Queries;

public sealed record GetNewsFeedQuery : IRequest<CursorPaginatedResult<NewsArticleListDto>>
{
    public string? SpaceSlug { get; init; }
    public string? ChannelSlug { get; init; }
    public string? TagSlug { get; init; }
    public ContentStatus? Status { get; init; }
    public bool? IsPinned { get; init; }
    public string? Search { get; init; }
    public string? Cursor { get; init; }
    public int Size { get; init; } = 20;
}
