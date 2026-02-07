using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class JourneyEnrollmentConfiguration : IEntityTypeConfiguration<JourneyEnrollment>
{
    public void Configure(EntityTypeBuilder<JourneyEnrollment> builder)
    {
        builder.ToTable("JourneyEnrollments");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Journey)
            .WithMany(j => j.Enrollments)
            .HasForeignKey(e => e.JourneyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: user can only be enrolled once per journey
        builder.HasIndex(e => new { e.JourneyId, e.UserId }).IsUnique();

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.StartedAt);
        builder.HasIndex(e => e.LastStepDeliveredAt);
    }
}
