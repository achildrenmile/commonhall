using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class SurveyQuestion : BaseEntity
{
    public Guid SurveyId { get; set; }
    public SurveyQuestionType Type { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Options { get; set; } // JSONB array of options
    public bool IsRequired { get; set; } = true;
    public int SortOrder { get; set; }
    public string? Settings { get; set; } // JSONB for type-specific settings

    // Navigation properties
    public Survey Survey { get; set; } = null!;
    public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
}
