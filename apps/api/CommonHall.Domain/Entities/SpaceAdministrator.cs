namespace CommonHall.Domain.Entities;

public sealed class SpaceAdministrator
{
    public Guid SpaceId { get; set; }
    public Guid UserId { get; set; }

    // Navigation properties
    public Space Space { get; set; } = null!;
    public User User { get; set; } = null!;
}
