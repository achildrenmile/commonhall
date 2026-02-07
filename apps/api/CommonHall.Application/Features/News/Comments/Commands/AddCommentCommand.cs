using CommonHall.Application.DTOs;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.News.Comments.Commands;

public sealed record AddCommentCommand : IRequest<CommentDto>
{
    public Guid NewsArticleId { get; init; }
    public required string Body { get; init; }
    public Guid? ParentCommentId { get; init; }
}

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.NewsArticleId)
            .NotEmpty().WithMessage("Article ID is required.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Comment body is required.")
            .MinimumLength(1).WithMessage("Comment body must not be empty.")
            .MaximumLength(5000).WithMessage("Comment body must not exceed 5000 characters.");
    }
}
