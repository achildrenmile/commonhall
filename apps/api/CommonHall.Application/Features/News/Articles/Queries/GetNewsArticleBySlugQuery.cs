using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.News.Articles.Queries;

public sealed record GetNewsArticleBySlugQuery : IRequest<NewsArticleDetailDto>
{
    public required string Slug { get; init; }
    public bool IncrementView { get; init; } = true;
}
