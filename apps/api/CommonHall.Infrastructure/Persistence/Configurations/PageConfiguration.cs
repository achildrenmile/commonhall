using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("Pages");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.Content)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(p => p.MetaDescription)
            .HasMaxLength(500);

        builder.Property(p => p.VisibilityRule)
            .HasColumnType("jsonb");

        builder.HasOne(p => p.Space)
            .WithMany(s => s.Pages)
            .HasForeignKey(p => p.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.SpaceId, p.Slug })
            .IsUnique();

        builder.HasIndex(p => p.SpaceId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.IsHomePage);
        builder.HasIndex(p => p.IsDeleted);
    }
}
