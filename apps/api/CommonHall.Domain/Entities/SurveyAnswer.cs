using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class SurveyAnswer : BaseEntity
{
    public Guid ResponseId { get; set; }
    public Guid QuestionId { get; set; }
    public string Value { get; set; } = "{}"; // JSONB

    // Navigation properties
    public SurveyResponse Response { get; set; } = null!;
    public SurveyQuestion Question { get; set; } = null!;
}
