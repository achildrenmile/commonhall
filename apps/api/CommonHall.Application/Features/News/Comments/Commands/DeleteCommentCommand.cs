using MediatR;

namespace CommonHall.Application.Features.News.Comments.Commands;

public sealed record DeleteCommentCommand(Guid Id) : IRequest;
