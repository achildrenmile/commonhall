using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class SurveyResponseConfiguration : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        builder.ToTable("SurveyResponses");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserHash)
            .HasMaxLength(128);

        builder.HasOne(r => r.Survey)
            .WithMany(s => s.Responses)
            .HasForeignKey(r => r.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique: one response per user per survey (for non-anonymous)
        builder.HasIndex(r => new { r.SurveyId, r.UserId })
            .HasFilter("\"UserId\" IS NOT NULL")
            .IsUnique();

        // For anonymous dedup
        builder.HasIndex(r => new { r.SurveyId, r.UserHash })
            .HasFilter("\"UserHash\" IS NOT NULL")
            .IsUnique();

        builder.HasIndex(r => r.IsComplete);
        builder.HasIndex(r => r.CompletedAt);
    }
}
