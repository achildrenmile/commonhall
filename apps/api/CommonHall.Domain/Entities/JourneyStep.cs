using CommonHall.Domain.Common;
using CommonHall.Domain.Enums;

namespace CommonHall.Domain.Entities;

public sealed class JourneyStep : BaseEntity
{
    public Guid JourneyId { get; set; }
    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Content { get; set; } = "[]"; // JSONB widgets
    public int DelayDays { get; set; }
    public JourneyChannelType ChannelType { get; set; } = JourneyChannelType.Both;
    public bool IsRequired { get; set; } = true;

    // Navigation properties
    public Journey Journey { get; set; } = null!;
    public ICollection<JourneyStepCompletion> Completions { get; set; } = new List<JourneyStepCompletion>();
}
