using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class SurveyResponse : BaseEntity
{
    public Guid SurveyId { get; set; }
    public Guid? UserId { get; set; } // Null for anonymous
    public string? UserHash { get; set; } // For anonymous dedup
    public bool IsComplete { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    // Navigation properties
    public Survey Survey { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
}
