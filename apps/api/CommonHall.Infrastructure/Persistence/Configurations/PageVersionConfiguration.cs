using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class PageVersionConfiguration : IEntityTypeConfiguration<PageVersion>
{
    public void Configure(EntityTypeBuilder<PageVersion> builder)
    {
        builder.ToTable("PageVersions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Content)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(v => v.ChangeDescription)
            .HasMaxLength(500);

        builder.HasOne(v => v.Page)
            .WithMany(p => p.Versions)
            .HasForeignKey(v => v.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.PageId);
        builder.HasIndex(v => new { v.PageId, v.VersionNumber })
            .IsUnique();
    }
}
