using CommonHall.Domain.Entities;

namespace CommonHall.Application.DTOs;

public sealed record CommentDto
{
    public required Guid Id { get; init; }
    public required UserSummaryDto Author { get; init; }
    public required string Body { get; init; }
    public Guid? ParentCommentId { get; init; }
    public required bool IsModerated { get; init; }
    public string? SentimentLabel { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required bool IsOwn { get; init; }
    public List<CommentDto>? Replies { get; init; }

    public static CommentDto FromEntity(Comment comment, UserSummaryDto author, Guid? currentUserId, List<CommentDto>? replies = null) => new()
    {
        Id = comment.Id,
        Author = author,
        Body = comment.IsModerated ? "[This comment has been moderated]" : comment.Body,
        ParentCommentId = comment.ParentCommentId,
        IsModerated = comment.IsModerated,
        SentimentLabel = comment.SentimentLabel,
        CreatedAt = comment.CreatedAt,
        IsOwn = currentUserId.HasValue && comment.AuthorId == currentUserId.Value,
        Replies = replies
    };
}
