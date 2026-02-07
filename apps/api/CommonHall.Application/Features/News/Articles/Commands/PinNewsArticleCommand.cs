using MediatR;

namespace CommonHall.Application.Features.News.Articles.Commands;

public sealed record PinNewsArticleCommand : IRequest
{
    public Guid Id { get; init; }
    public bool IsPinned { get; init; }
}
