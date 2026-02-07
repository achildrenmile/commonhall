namespace CommonHall.Domain.Entities;

public sealed class EmailClick
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid RecipientId { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset ClickedAt { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }

    // Navigation properties
    public EmailRecipient Recipient { get; set; } = null!;
}
