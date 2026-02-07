using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.News.Comments.Queries;

public sealed record GetCommentsQuery : IRequest<CursorPaginatedResult<CommentDto>>
{
    public Guid NewsArticleId { get; init; }
    public string? Cursor { get; init; }
    public int Size { get; init; } = 20;
}
