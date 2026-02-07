namespace CommonHall.Application.DTOs;

public sealed record ReactionSummaryDto
{
    public required int TotalLikes { get; init; }
    public required bool UserHasLiked { get; init; }
}

public sealed record ToggleReactionResultDto
{
    public required bool IsReacted { get; init; }
    public required int TotalCount { get; init; }
}
