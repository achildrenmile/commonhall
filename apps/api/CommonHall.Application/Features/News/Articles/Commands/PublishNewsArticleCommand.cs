using CommonHall.Application.DTOs;
using MediatR;

namespace CommonHall.Application.Features.News.Articles.Commands;

public sealed record PublishNewsArticleCommand(Guid Id) : IRequest<NewsArticleDto>;
