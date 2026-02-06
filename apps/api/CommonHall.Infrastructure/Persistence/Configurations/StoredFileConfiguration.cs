using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.OriginalName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.MimeType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.StoragePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(f => f.ThumbnailPath)
            .HasMaxLength(1000);

        builder.Property(f => f.AltText)
            .HasMaxLength(500);

        builder.HasOne(f => f.Collection)
            .WithMany(c => c.Files)
            .HasForeignKey(f => f.CollectionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(f => f.Uploader)
            .WithMany(u => u.UploadedFiles)
            .HasForeignKey(f => f.UploadedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.CollectionId);
        builder.HasIndex(f => f.UploadedBy);
        builder.HasIndex(f => f.MimeType);
    }
}
