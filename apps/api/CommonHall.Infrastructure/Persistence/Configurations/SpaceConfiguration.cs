using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class SpaceConfiguration : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> builder)
    {
        builder.ToTable("Spaces");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Slug)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(2000);

        builder.Property(s => s.IconUrl)
            .HasMaxLength(500);

        builder.Property(s => s.CoverImageUrl)
            .HasMaxLength(500);

        builder.HasOne(s => s.ParentSpace)
            .WithMany(s => s.ChildSpaces)
            .HasForeignKey(s => s.ParentSpaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.Slug)
            .IsUnique();

        builder.HasIndex(s => s.ParentSpaceId);
        builder.HasIndex(s => s.IsDefault);
        builder.HasIndex(s => s.IsDeleted);
    }
}
