using CommonHall.Domain.Common;

namespace CommonHall.Domain.Entities;

public sealed class FormSubmission : BaseEntity
{
    public Guid FormId { get; set; }
    public Guid? UserId { get; set; }
    public string Data { get; set; } = "{}"; // JSONB
    public string? Attachments { get; set; } // JSONB array of file references

    // Navigation properties
    public Form Form { get; set; } = null!;
    public User? User { get; set; }
}
