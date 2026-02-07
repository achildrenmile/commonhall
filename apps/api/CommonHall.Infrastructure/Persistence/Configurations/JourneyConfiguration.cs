using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class JourneyConfiguration : IEntityTypeConfiguration<Journey>
{
    public void Configure(EntityTypeBuilder<Journey> builder)
    {
        builder.ToTable("Journeys");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.Description)
            .HasMaxLength(2000);

        builder.Property(j => j.TriggerConfig)
            .HasColumnType("jsonb");

        builder.HasOne(j => j.Space)
            .WithMany()
            .HasForeignKey(j => j.SpaceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(j => j.TriggerType);
        builder.HasIndex(j => j.IsActive);
        builder.HasIndex(j => j.SpaceId);
        builder.HasIndex(j => j.IsDeleted);
    }
}
