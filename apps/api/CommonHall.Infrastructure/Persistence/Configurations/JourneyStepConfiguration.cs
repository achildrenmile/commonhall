using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class JourneyStepConfiguration : IEntityTypeConfiguration<JourneyStep>
{
    public void Configure(EntityTypeBuilder<JourneyStep> builder)
    {
        builder.ToTable("JourneySteps");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(2000);

        builder.Property(s => s.Content)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.HasOne(s => s.Journey)
            .WithMany(j => j.Steps)
            .HasForeignKey(s => s.JourneyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.JourneyId, s.SortOrder });
    }
}
