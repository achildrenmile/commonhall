using MediatR;

namespace CommonHall.Application.Features.News.Articles.Commands;

public sealed record DeleteNewsArticleCommand(Guid Id) : IRequest;
