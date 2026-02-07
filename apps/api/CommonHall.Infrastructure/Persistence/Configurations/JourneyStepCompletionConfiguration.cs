using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class JourneyStepCompletionConfiguration : IEntityTypeConfiguration<JourneyStepCompletion>
{
    public void Configure(EntityTypeBuilder<JourneyStepCompletion> builder)
    {
        builder.ToTable("JourneyStepCompletions");

        builder.HasKey(c => c.Id);

        builder.HasOne(c => c.Enrollment)
            .WithMany(e => e.StepCompletions)
            .HasForeignKey(c => c.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.EnrollmentId, c.StepIndex }).IsUnique();
        builder.HasIndex(c => c.DeliveredAt);
        builder.HasIndex(c => c.CompletedAt);
    }
}
