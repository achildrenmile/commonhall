using MediatR;

namespace CommonHall.Application.Features.News.Comments.Commands;

public sealed record ModerateCommentCommand : IRequest
{
    public Guid Id { get; init; }
    public bool IsModerated { get; init; }
}
