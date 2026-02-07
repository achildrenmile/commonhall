using MediatR;

namespace CommonHall.Application.Features.News.Channels.Commands;

public sealed record DeleteNewsChannelCommand(Guid Id) : IRequest;
