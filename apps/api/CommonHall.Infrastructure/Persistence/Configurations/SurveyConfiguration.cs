using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        builder.ToTable("Surveys");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Description)
            .HasMaxLength(2000);

        builder.Property(s => s.RecurrenceConfig)
            .HasColumnType("jsonb");

        builder.Property(s => s.TargetGroupIds)
            .HasColumnType("jsonb");

        builder.HasOne(s => s.Space)
            .WithMany()
            .HasForeignKey(s => s.SpaceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.StartsAt);
        builder.HasIndex(s => s.EndsAt);
        builder.HasIndex(s => s.SpaceId);
        builder.HasIndex(s => s.IsDeleted);
    }
}
