using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class TrackingEventConfiguration : IEntityTypeConfiguration<TrackingEvent>
{
    public void Configure(EntityTypeBuilder<TrackingEvent> builder)
    {
        builder.ToTable("TrackingEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.TargetType)
            .HasMaxLength(50);

        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb");

        builder.Property(e => e.Channel)
            .HasMaxLength(50);

        builder.Property(e => e.DeviceType)
            .HasMaxLength(50);

        builder.Property(e => e.SessionId)
            .HasMaxLength(100);

        builder.Property(e => e.Timestamp)
            .IsRequired();

        // Indexes for efficient querying
        builder.HasIndex(e => new { e.EventType, e.Timestamp });
        builder.HasIndex(e => new { e.TargetType, e.TargetId, e.Timestamp });
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.SessionId);
        builder.HasIndex(e => e.Timestamp);

        // Relationships
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
