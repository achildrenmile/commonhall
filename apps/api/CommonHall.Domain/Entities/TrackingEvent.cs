using System;

namespace CommonHall.Domain.Entities;

public sealed class TrackingEvent
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? Metadata { get; set; }
    public string? Channel { get; set; }
    public string? DeviceType { get; set; }
    public string? SessionId { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    // Navigation
    public User? User { get; set; }
}
