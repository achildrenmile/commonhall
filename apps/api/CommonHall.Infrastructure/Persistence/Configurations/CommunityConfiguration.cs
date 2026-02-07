using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class CommunityConfiguration : IEntityTypeConfiguration<Community>
{
    public void Configure(EntityTypeBuilder<Community> builder)
    {
        builder.ToTable("Communities");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(2000);

        builder.Property(c => c.CoverImageUrl)
            .HasMaxLength(500);

        builder.Property(c => c.AssignedGroupIds)
            .HasColumnType("jsonb");

        builder.HasOne(c => c.Space)
            .WithMany()
            .HasForeignKey(c => c.SpaceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.Type);
        builder.HasIndex(c => c.IsArchived);
        builder.HasIndex(c => c.IsDeleted);
    }
}
