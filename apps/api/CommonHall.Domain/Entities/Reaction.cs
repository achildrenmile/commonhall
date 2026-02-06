using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class Reaction
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid NewsArticleId { get; set; }
    public Guid UserId { get; set; }
    public ReactionType Type { get; set; } = ReactionType.Like;
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation properties
    public NewsArticle NewsArticle { get; set; } = null!;
    public User User { get; set; } = null!;
}
